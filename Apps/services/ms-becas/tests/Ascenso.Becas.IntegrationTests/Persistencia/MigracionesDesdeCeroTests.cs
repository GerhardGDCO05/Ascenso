using Ascenso.Becas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class MigracionesDesdeCeroTests
{
    private static readonly string[] _tablasEsperadas =
    [
        "beca",
        "compromiso_becario",
        "convenio_ref",
        "costo_matricula_ref",
        "historial_estado_beca",
        "idempotency_request",
        "inbox_state",
        "oferta_beca",
        "outbox_message",
        "outbox_state",
        "programa_beca_ref"
    ];

    private static readonly string[] _indicesEsperados =
    [
        "ix_historial_estado_beca_beca_id",
        "ux_beca_cadena_periodo",
        "ux_beca_codigo",
        "ux_beca_estudiante_cobertura_total_activa",
        "ux_beca_estudiante_periodo_no_acumulable_activa",
        "ux_compromiso_becario_beca",
        "ux_idempotency_request_endpoint_clave",
        "ux_oferta_beca_beca"
    ];

    private static readonly string[] _checksEsperados =
    [
        "ck_beca_cobertura_porcentaje_valido",
        "ck_beca_estado_valido",
        "ck_compromiso_becario_estado_valido",
        "ck_costo_matricula_ref_monto_no_negativo",
        "ck_historial_estado_beca_tipo_entidad_valido",
        "ck_idempotency_request_expiracion_valida",
        "ck_oferta_beca_estado_valido",
        "ck_programa_beca_ref_cobertura_porcentaje_valido"
    ];

    private readonly PostgreSqlFixture _fixture;

    public MigracionesDesdeCeroTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_CrearElEsquemaDesdeCero_Y_AplicarLasMigracionesDeFormaIdempotente()
    {
        var nombreBase = $"becas_migraciones_{Guid.NewGuid():N}";
        await CrearBaseDeDatosAsync(nombreBase);

        await using var contexto = CrearContexto(nombreBase);

        await contexto.Database.MigrateAsync();
        var migracionesAplicadas = (await contexto.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.NotEmpty(migracionesAplicadas);
        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());

        await contexto.Database.MigrateAsync();
        var migracionesTrasSegundaEjecucion =
            (await contexto.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.Equal(migracionesAplicadas, migracionesTrasSegundaEjecucion);
        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());

        var tablas = await ConsultarNombresAsync(
            contexto,
            """
            SELECT table_name AS "Value"
            FROM information_schema.tables
            WHERE table_schema = 'becas'
            """);
        Assert.Empty(_tablasEsperadas.Except(tablas));

        var indices = await ConsultarNombresAsync(
            contexto,
            """
            SELECT indexname AS "Value"
            FROM pg_indexes
            WHERE schemaname = 'becas'
            """);
        Assert.Empty(_indicesEsperados.Except(indices));

        var checks = await ConsultarNombresAsync(
            contexto,
            """
            SELECT restriccion.conname AS "Value"
            FROM pg_constraint AS restriccion
            INNER JOIN pg_namespace AS espacio
                ON espacio.oid = restriccion.connamespace
            WHERE espacio.nspname = 'becas'
              AND restriccion.contype = 'c'
            """);
        Assert.Empty(_checksEsperados.Except(checks));

        var funciones = await ConsultarNombresAsync(
            contexto,
            """
            SELECT funcion.proname AS "Value"
            FROM pg_proc AS funcion
            INNER JOIN pg_namespace AS espacio
                ON espacio.oid = funcion.pronamespace
            WHERE espacio.nspname = 'becas'
              AND funcion.proname = 'fn_historial_estado_beca_inmutable'
            """);
        Assert.Contains("fn_historial_estado_beca_inmutable", funciones);

        var triggers = await ConsultarNombresAsync(
            contexto,
            """
            SELECT trigger.tgname AS "Value"
            FROM pg_trigger AS trigger
            INNER JOIN pg_class AS tabla
                ON tabla.oid = trigger.tgrelid
            INNER JOIN pg_namespace AS espacio
                ON espacio.oid = tabla.relnamespace
            WHERE espacio.nspname = 'becas'
              AND NOT trigger.tgisinternal
            """);
        Assert.Contains("trg_historial_estado_beca_inmutable", triggers);

        await VerificarCheckAsync(contexto);
    }

    private BecasDbContext CrearContexto(string nombreBase)
    {
        var cadenaConexion = new NpgsqlConnectionStringBuilder(_fixture.CadenaConexion)
        {
            Database = nombreBase
        };

        var opciones = new DbContextOptionsBuilder<BecasDbContext>()
            .UseNpgsql(
                cadenaConexion.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    BecasDbContext.Esquema))
            .Options;

        return new BecasDbContext(opciones);
    }

    private async Task CrearBaseDeDatosAsync(string nombreBase)
    {
        await using var conexion = new NpgsqlConnection(_fixture.CadenaConexion);
        await conexion.OpenAsync();

#pragma warning disable CA2100 // El nombre se genera con un GUID y solo contiene caracteres seguros.
        await using var comando = new NpgsqlCommand($"CREATE DATABASE {nombreBase}", conexion);
#pragma warning restore CA2100
        await comando.ExecuteNonQueryAsync();
    }

    private static Task<List<string>> ConsultarNombresAsync(BecasDbContext contexto, string sql)
        => contexto.Database.SqlQueryRaw<string>(sql).ToListAsync();

    private static async Task VerificarCheckAsync(BecasDbContext contexto)
    {
        var ahora = DateTimeOffset.UtcNow;
        var excepcion = await Assert.ThrowsAsync<PostgresException>(
            () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO becas.idempotency_request (
                    id, endpoint, clave, hash_payload, estado, creado_en, expira_en)
                VALUES (
                    {Guid.CreateVersion7()}, '/prueba', 'clave',
                    {new string('f', 64)}, 'PENDIENTE', {ahora}, {ahora})
                """));

        Assert.Equal(PostgresErrorCodes.CheckViolation, excepcion.SqlState);
        Assert.Equal("ck_idempotency_request_expiracion_valida", excepcion.ConstraintName);
    }
}
