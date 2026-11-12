using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.ValueObjects;
using Ascenso.Becas.Infrastructure.Persistence;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class RestriccionesCheckTests
{
    private readonly PostgreSqlFixture _fixture;

    public RestriccionesCheckTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("estado = 'INVALIDO'", "ck_beca_estado_valido")]
    [InlineData("tipo_cierre = 'INVALIDO'", "ck_beca_tipo_cierre_valido")]
    [InlineData("cobertura_modalidad = 'INVALIDA'", "ck_beca_cobertura_modalidad_valida")]
    [InlineData("cobertura_porcentaje = 101", "ck_beca_cobertura_porcentaje_valido")]
    [InlineData("cobertura_porcentaje = -0.01", "ck_beca_cobertura_porcentaje_valido")]
    [InlineData("monto_comprometido_periodo_actual = -1", "ck_beca_montos_no_negativos")]
    [InlineData("monto_desembolsado_periodo_actual = -1", "ck_beca_montos_no_negativos")]
    [InlineData("cobertura_monto_fijo_periodo = -1", "ck_beca_montos_no_negativos")]
    [InlineData("periodos_cursados = -1", "ck_beca_periodos_validos")]
    [InlineData("duracion_total_periodos = 0", "ck_beca_periodos_validos")]
    [InlineData("periodos_cursados = duracion_total_periodos + 1", "ck_beca_periodos_validos")]
    [InlineData("fecha_activacion = fecha_otorgamiento - 1", "ck_beca_fechas_coherentes")]
    [InlineData("fecha_conclusion_o_revocacion = fecha_otorgamiento - 1", "ck_beca_fechas_coherentes")]
    public async Task Debe_RechazarUnaBecaConDatosInvalidos(
        string asignaciones,
        string restriccion)
    {
        await using var contexto = _fixture.CrearContexto();
        var beca = await CrearBecaAsync(contexto);

        var excepcion = await EjecutarUpdateInvalidoAsync(
            contexto,
            "becas.beca",
            "id",
            beca.Id,
            asignaciones);

        AssertCheckViolation(excepcion, restriccion);
    }

    [Theory]
    [InlineData("estado = 'INVALIDO'", "ck_oferta_beca_estado_valido")]
    [InlineData("monto_oferta_por_periodo = -1", "ck_oferta_beca_montos_no_negativos")]
    [InlineData("monto_total_estimado_duracion = -1", "ck_oferta_beca_montos_no_negativos")]
    [InlineData("duracion_periodos = 0", "ck_oferta_beca_duracion_valida")]
    [InlineData("fecha_vencimiento_aceptacion = fecha_emision", "ck_oferta_beca_fechas_coherentes")]
    [InlineData("fecha_respuesta = fecha_emision - interval '1 second'", "ck_oferta_beca_fechas_coherentes")]
    [InlineData("condiciones_promedio_minimo_renovacion = 21", "ck_oferta_beca_condiciones_validas")]
    [InlineData("condiciones_consecuencia_incumplimiento = 'INVALIDA'", "ck_oferta_beca_condiciones_validas")]
    public async Task Debe_RechazarUnaOfertaConDatosInvalidos(
        string asignaciones,
        string restriccion)
    {
        await using var contexto = _fixture.CrearContexto();
        var beca = await CrearBecaConOfertaAsync(contexto);

        var excepcion = await EjecutarUpdateInvalidoAsync(
            contexto,
            "becas.oferta_beca",
            "beca_id",
            beca.Id,
            asignaciones);

        AssertCheckViolation(excepcion, restriccion);
    }

    [Theory]
    [InlineData("estado = 'INVALIDO'", "ck_compromiso_becario_estado_valido")]
    [InlineData("numero_intento_carga = -1", "ck_compromiso_becario_intento_valido")]
    public async Task Debe_RechazarUnCompromisoConDatosInvalidos(
        string asignaciones,
        string restriccion)
    {
        await using var contexto = _fixture.CrearContexto();
        var beca = await CrearBecaConCompromisoAsync(contexto);

        var excepcion = await EjecutarUpdateInvalidoAsync(
            contexto,
            "becas.compromiso_becario",
            "beca_id",
            beca.Id,
            asignaciones);

        AssertCheckViolation(excepcion, restriccion);
    }

    [Theory]
    [InlineData("politica_cobertura_modalidad = 'INVALIDA'", "ck_programa_beca_ref_cobertura_modalidad_valida")]
    [InlineData("politica_cobertura_porcentaje = 101", "ck_programa_beca_ref_cobertura_porcentaje_valido")]
    [InlineData("politica_cobertura_monto_fijo_periodo = -1", "ck_programa_beca_ref_montos_no_negativos")]
    [InlineData("duracion_periodos = 0", "ck_programa_beca_ref_duracion_valida")]
    [InlineData("condiciones_permanencia_promedio_minimo_renovacion = 21", "ck_programa_beca_ref_condiciones_validas")]
    public async Task Debe_RechazarUnaProyeccionDeProgramaConDatosInvalidos(
        string asignaciones,
        string restriccion)
    {
        await using var contexto = _fixture.CrearContexto();
        var programa = CrearProgramaRef();
        contexto.ProgramasBecaRef.Add(programa);
        await contexto.SaveChangesAsync();

        var excepcion = await EjecutarUpdateInvalidoAsync(
            contexto,
            "becas.programa_beca_ref",
            "programa_beca_id",
            programa.ProgramaBecaId,
            asignaciones);

        AssertCheckViolation(excepcion, restriccion);
    }

    [Theory]
    [InlineData("monto_referencia_matricula = -1", "ck_costo_matricula_ref_monto_no_negativo")]
    [InlineData("numero_version = 0", "ck_costo_matricula_ref_version_valida")]
    [InlineData("fecha_vigencia_hasta = fecha_vigencia_desde - 1", "ck_costo_matricula_ref_fechas_coherentes")]
    public async Task Debe_RechazarUnaProyeccionDeCostoConDatosInvalidos(
        string asignaciones,
        string restriccion)
    {
        await using var contexto = _fixture.CrearContexto();
        var costo = CrearCostoRef();
        contexto.CostosMatriculaRef.Add(costo);
        await contexto.SaveChangesAsync();

        var excepcion = await EjecutarUpdateInvalidoAsync(
            contexto,
            "becas.costo_matricula_ref",
            "costo_matricula_referencia_id",
            costo.CostoMatriculaReferenciaId,
            asignaciones);

        AssertCheckViolation(excepcion, restriccion);
    }

    [Theory]
    [InlineData("status_code = 99", "ck_idempotency_request_status_code_valido")]
    [InlineData("status_code = 600", "ck_idempotency_request_status_code_valido")]
    [InlineData("expira_en = creado_en", "ck_idempotency_request_expiracion_valida")]
    [InlineData("expira_en = creado_en - interval '1 second'", "ck_idempotency_request_expiracion_valida")]
    public async Task Debe_RechazarUnaSolicitudIdempotenteConDatosInvalidos(
        string asignaciones,
        string restriccion)
    {
        await using var contexto = _fixture.CrearContexto();
        var solicitud = CrearSolicitudIdempotente();
        contexto.SolicitudesIdempotencia.Add(solicitud);
        await contexto.SaveChangesAsync();

        var excepcion = await EjecutarUpdateInvalidoAsync(
            contexto,
            "becas.idempotency_request",
            "id",
            solicitud.Id,
            asignaciones);

        AssertCheckViolation(excepcion, restriccion);
    }

    private static async Task<Beca> CrearBecaAsync(BecasDbContext contexto)
    {
        var beca = Beca.CrearOtorgada(
            DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());

        contexto.Becas.Add(beca);
        await contexto.SaveChangesAsync();

        return beca;
    }

    private static async Task<Beca> CrearBecaConOfertaAsync(BecasDbContext contexto)
    {
        var datosBeca = DatosPrueba.CrearDatosBeca();
        var contextoOperacion = DatosPrueba.CrearContextoOperacion();
        var beca = Beca.CrearOtorgada(datosBeca, contextoOperacion);
        beca.EmitirOferta(DatosPrueba.CrearDatosOferta(datosBeca), contextoOperacion);

        contexto.Becas.Add(beca);
        await contexto.SaveChangesAsync();

        return beca;
    }

    private static async Task<Beca> CrearBecaConCompromisoAsync(BecasDbContext contexto)
    {
        var beca = Beca.CrearOtorgada(
            DatosPrueba.CrearDatosBeca(requiereCompromisoFirmado: true),
            DatosPrueba.CrearContextoOperacion());

        contexto.Becas.Add(beca);
        await contexto.SaveChangesAsync();

        return beca;
    }

    private static ProgramaBecaRef CrearProgramaRef()
        => new()
        {
            ProgramaBecaId = Guid.CreateVersion7(),
            Codigo = $"PROG-{Guid.NewGuid():N}"[..20],
            Nombre = "Programa de prueba",
            PoliticaCobertura = new PoliticaCobertura(
                TipoCobertura.Parcial,
                50m,
                0m,
                "USD"),
            CondicionesPermanencia = new CondicionesPermanencia(
                13m,
                4,
                1,
                40,
                ConsecuenciaPermanencia.LlamadoAtencion),
            PermiteAcumulacion = true,
            RequiereCompromisoFirmado = false,
            DuracionPeriodos = 2,
            Estado = "ACTIVO",
            UltimoEventoId = Guid.CreateVersion7(),
            OccurredAt = DateTimeOffset.UtcNow,
            FechaUltimaActualizacion = DateTimeOffset.UtcNow
        };

    private static CostoMatriculaRef CrearCostoRef()
        => new()
        {
            CostoMatriculaReferenciaId = Guid.CreateVersion7(),
            CarreraId = Guid.CreateVersion7(),
            PeriodoAcademicoId = Guid.CreateVersion7(),
            MontoReferenciaMatricula = 1200m,
            Moneda = "USD",
            NumeroVersion = 1,
            FechaVigenciaDesde = new DateOnly(2026, 1, 1),
            FechaVigenciaHasta = new DateOnly(2026, 12, 31),
            Estado = "VIGENTE",
            UltimoEventoId = Guid.CreateVersion7(),
            OccurredAt = DateTimeOffset.UtcNow,
            FechaUltimaActualizacion = DateTimeOffset.UtcNow
        };

    private static IdempotencyRequest CrearSolicitudIdempotente()
    {
        var creadoEn = DateTimeOffset.UtcNow;

        return new IdempotencyRequest
        {
            Id = Guid.CreateVersion7(),
            Endpoint = $"/api/v1/becas/{Guid.NewGuid():N}",
            Clave = Guid.NewGuid().ToString("N"),
            HashPayload = new string('c', 64),
            Estado = "COMPLETADA",
            StatusCode = 200,
            ResponseBody = """{"ok":true}""",
            RecursoId = Guid.CreateVersion7(),
            CreadoEn = creadoEn,
            ExpiraEn = creadoEn.AddDays(7)
        };
    }

    private static async Task<PostgresException> EjecutarUpdateInvalidoAsync(
        BecasDbContext contexto,
        string tabla,
        string columnaClave,
        Guid id,
        string asignaciones)
    {
#pragma warning disable CA2100 // Los fragmentos SQL provienen de literales controlados por la prueba.
        var sql = $"UPDATE {tabla} SET {asignaciones} WHERE {columnaClave} = '{id}'";
#pragma warning restore CA2100

        return await Assert.ThrowsAsync<PostgresException>(
            () => contexto.Database.ExecuteSqlRawAsync(sql));
    }

    private static void AssertCheckViolation(
        PostgresException excepcion,
        string restriccion)
    {
        Assert.Equal(PostgresErrorCodes.CheckViolation, excepcion.SqlState);
        Assert.Equal(restriccion, excepcion.ConstraintName);
    }
}
