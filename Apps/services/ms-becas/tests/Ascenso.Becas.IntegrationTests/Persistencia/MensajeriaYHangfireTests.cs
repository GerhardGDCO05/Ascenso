using Ascenso.Becas.Infrastructure.Dependencias;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class MensajeriaYHangfireTests
{
    private readonly PostgreSqlFixture _fixture;

    public MensajeriaYHangfireTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_CrearLasTablasDeOutboxEInbox_Cuando_SeAplicanLasMigraciones()
    {
        await using var contexto = _fixture.CrearContexto();

        var tablas = await contexto.Database
            .SqlQuery<string>($"""
                SELECT table_name AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'becas'
                  AND table_name IN ('inbox_state', 'outbox_state', 'outbox_message')
                ORDER BY table_name
                """)
            .ToListAsync();

        Assert.Equal(
            ["inbox_state", "outbox_message", "outbox_state"],
            tablas);
    }

    [Fact]
    public async Task Debe_PermitirEscribirEnLasTablasDeOutboxEInbox()
    {
        await using var contexto = _fixture.CrearContexto();
        var outboxId = Guid.CreateVersion7();
        var lockId = Guid.CreateVersion7();
        var messageId = Guid.CreateVersion7();
        var consumerId = Guid.CreateVersion7();
        var cuerpo = "{}";

        await contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO becas.outbox_state ("OutboxId", "LockId", "Created")
            VALUES ({outboxId}, {lockId}, NOW());

            INSERT INTO becas.outbox_message (
                "SentTime", "MessageId", "ContentType", "MessageType", "Body", "OutboxId")
            VALUES (
                NOW(), {messageId}, 'application/json', 'EventoDePrueba', {cuerpo}, {outboxId});

            INSERT INTO becas.inbox_state (
                "MessageId", "ConsumerId", "LockId", "Received", "ReceiveCount")
            VALUES ({messageId}, {consumerId}, {lockId}, NOW(), 1);
            """);

        var outbox = await contexto.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*)::int AS "Value"
                FROM becas.outbox_message
                WHERE "OutboxId" = {outboxId}
                """)
            .SingleAsync();

        var inbox = await contexto.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*)::int AS "Value"
                FROM becas.inbox_state
                WHERE "MessageId" = {messageId} AND "ConsumerId" = {consumerId}
                """)
            .SingleAsync();

        Assert.Equal(1, outbox);
        Assert.Equal(1, inbox);
    }

    [Fact]
    public async Task Debe_CrearElEsquemaDeHangfireYPermitirEncolarUnJob()
    {
        var servicios = new ServiceCollection();
        servicios.AgregarHangfire(_fixture.CadenaConexion);

        await using var proveedor = servicios.BuildServiceProvider();
        var almacenamiento = proveedor.GetRequiredService<JobStorage>();
        var cliente = new BackgroundJobClient(almacenamiento);

        var jobId = cliente.Enqueue<TrabajoDePrueba>(
            trabajo => trabajo.Ejecutar());

        Assert.False(string.IsNullOrWhiteSpace(jobId));

        await using var conexion = new NpgsqlConnection(_fixture.CadenaConexion);
        await conexion.OpenAsync();

        var tablas = await ObtenerTablasHangfireAsync(conexion);
        Assert.Contains("job", tablas);
        Assert.Contains("jobqueue", tablas);
        Assert.Contains("state", tablas);

        Assert.True(await ContarAsync(
            conexion,
            "SELECT COUNT(*) FROM hangfire.job"));
        Assert.True(await ContarAsync(
            conexion,
            "SELECT COUNT(*) FROM hangfire.state"));
    }

    private static async Task<HashSet<string>> ObtenerTablasHangfireAsync(
        NpgsqlConnection conexion)
    {
        await using var comando = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'hangfire'
            """,
            conexion);

        var tablas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            tablas.Add(lector.GetString(0));
        }

        return tablas;
    }

    private static async Task<bool> ContarAsync(
        NpgsqlConnection conexion,
        string sql)
    {
        await using var comando = new NpgsqlCommand(sql, conexion);
        var total = Convert.ToInt64(
            await comando.ExecuteScalarAsync(),
            System.Globalization.CultureInfo.InvariantCulture);

        return total > 0;
    }

    public sealed class TrabajoDePrueba
    {
        public void Ejecutar()
        {
        }
    }
}
