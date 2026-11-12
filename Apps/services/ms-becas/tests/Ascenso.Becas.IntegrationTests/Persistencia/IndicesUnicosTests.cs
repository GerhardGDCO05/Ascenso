using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class IndicesUnicosTests
{
    private readonly PostgreSqlFixture _fixture;

    public IndicesUnicosTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_RechazarCodigoBecaDuplicado_Cuando_SeGuardaOtraBeca()
    {
        await using var contexto = _fixture.CrearContexto();
        var codigo = DatosPrueba.NuevoCodigoBeca();

        await GuardarAsync(contexto, CrearBecaOtorgada(codigoBeca: codigo));

        contexto.Becas.Add(CrearBecaOtorgada(codigoBeca: codigo));

        var excepcion = await Assert.ThrowsAsync<DbUpdateException>(
            () => contexto.SaveChangesAsync());

        AssertUniqueViolation(excepcion, "ux_beca_codigo");
    }

    [Fact]
    public async Task Debe_RechazarDosBecasActivasConCoberturaTotalDelMismoEstudiante()
    {
        await using var contexto = _fixture.CrearContexto();
        var estudianteId = Guid.CreateVersion7();

        var primera = DatosPrueba.CrearBecaActiva(
            DatosPrueba.CrearDatosBeca(
                estudianteId: estudianteId,
                modalidad: TipoCobertura.Total,
                permiteAcumulacion: true),
            DatosPrueba.CrearContextoOperacion());

        await GuardarAsync(contexto, primera);

        var segunda = DatosPrueba.CrearBecaActiva(
            DatosPrueba.CrearDatosBeca(
                estudianteId: estudianteId,
                modalidad: TipoCobertura.Total,
                permiteAcumulacion: true),
            DatosPrueba.CrearContextoOperacion());

        contexto.Becas.Add(segunda);

        var excepcion = await Assert.ThrowsAsync<DbUpdateException>(
            () => contexto.SaveChangesAsync());

        AssertUniqueViolation(excepcion, "ux_beca_estudiante_cobertura_total_activa");
    }

    [Fact]
    public async Task Debe_RechazarDosBecasActivasNoAcumulablesDelMismoEstudianteYPeriodo()
    {
        await using var contexto = _fixture.CrearContexto();
        var estudianteId = Guid.CreateVersion7();
        var periodoId = Guid.CreateVersion7();

        var primera = DatosPrueba.CrearBecaActiva(
            DatosPrueba.CrearDatosBeca(
                estudianteId: estudianteId,
                periodoId: periodoId,
                modalidad: TipoCobertura.Parcial,
                permiteAcumulacion: false),
            DatosPrueba.CrearContextoOperacion());

        await GuardarAsync(contexto, primera);

        var segunda = DatosPrueba.CrearBecaActiva(
            DatosPrueba.CrearDatosBeca(
                estudianteId: estudianteId,
                periodoId: periodoId,
                modalidad: TipoCobertura.Parcial,
                permiteAcumulacion: false),
            DatosPrueba.CrearContextoOperacion());

        contexto.Becas.Add(segunda);

        var excepcion = await Assert.ThrowsAsync<DbUpdateException>(
            () => contexto.SaveChangesAsync());

        AssertUniqueViolation(excepcion, "ux_beca_estudiante_periodo_no_acumulable_activa");
    }

    [Fact]
    public async Task Debe_RechazarDosBecasDeLaMismaCadenaYPeriodo_Cuando_SeDuplicaLaClave()
    {
        await using var contexto = _fixture.CrearContexto();

        var primera = CrearBecaOtorgada();
        var segunda = CrearBecaOtorgada();

        await GuardarAsync(contexto, primera);
        await GuardarAsync(contexto, segunda);

        var excepcion = await Assert.ThrowsAsync<PostgresException>(
            () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE becas.beca
                SET cadena_beca_id = {primera.CadenaBecaId},
                    periodo_vigente_actual_id = {primera.PeriodoVigenteActualId}
                WHERE id = {segunda.Id}
                """));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, excepcion.SqlState);
        Assert.Equal("ux_beca_cadena_periodo", excepcion.ConstraintName);
    }

    [Fact]
    public async Task Debe_RechazarDosOfertasParaLaMismaBeca_Cuando_SeDuplicaLaOferta()
    {
        await using var contexto = _fixture.CrearContexto();

        var datosBeca = DatosPrueba.CrearDatosBeca();
        var contextoOperacion = DatosPrueba.CrearContextoOperacion();
        var beca = Beca.CrearOtorgada(datosBeca, contextoOperacion);
        var oferta = beca.EmitirOferta(
            DatosPrueba.CrearDatosOferta(datosBeca),
            contextoOperacion);

        await GuardarAsync(contexto, beca);

        var excepcion = await Assert.ThrowsAsync<PostgresException>(
            () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO becas.oferta_beca (
                    id, beca_id, postulacion_id, reserva_presupuesto_id,
                    monto_oferta_por_periodo, monto_total_estimado_duracion,
                    duracion_periodos, condiciones_promedio_minimo_renovacion,
                    condiciones_materias_minimas_aprobadas,
                    condiciones_maximo_materias_reprobadas,
                    condiciones_horas_servicio_por_periodo,
                    condiciones_consecuencia_incumplimiento,
                    url_carta_pdf_firebase, hash_carta_sha256,
                    fecha_emision, fecha_vencimiento_aceptacion, estado,
                    motivo_rechazo_o_anulacion, fecha_respuesta,
                    referencia_job_expiracion_hangfire_id,
                    emitida_por_usuario_id, autorizacion_otp_emision_id,
                    autorizacion_otp_aceptacion_id, fecha_creacion,
                    usuario_creacion_id, fecha_ultima_modificacion,
                    usuario_modificacion_id, version)
                SELECT
                    gen_random_uuid(), beca_id, postulacion_id,
                    reserva_presupuesto_id, monto_oferta_por_periodo,
                    monto_total_estimado_duracion, duracion_periodos,
                    condiciones_promedio_minimo_renovacion,
                    condiciones_materias_minimas_aprobadas,
                    condiciones_maximo_materias_reprobadas,
                    condiciones_horas_servicio_por_periodo,
                    condiciones_consecuencia_incumplimiento,
                    url_carta_pdf_firebase, hash_carta_sha256,
                    fecha_emision, fecha_vencimiento_aceptacion, estado,
                    motivo_rechazo_o_anulacion, fecha_respuesta,
                    referencia_job_expiracion_hangfire_id,
                    emitida_por_usuario_id, autorizacion_otp_emision_id,
                    autorizacion_otp_aceptacion_id, fecha_creacion,
                    usuario_creacion_id, fecha_ultima_modificacion,
                    usuario_modificacion_id, version
                FROM becas.oferta_beca
                WHERE id = {oferta.Id}
                """));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, excepcion.SqlState);
        Assert.Equal("ux_oferta_beca_beca", excepcion.ConstraintName);
    }

    [Fact]
    public async Task Debe_PermitirCoberturaTotalYActiva_Cuando_SoloExisteUnaBecaTotalActiva()
    {
        await using var contexto = _fixture.CrearContexto();
        var estudianteId = Guid.CreateVersion7();

        var total = DatosPrueba.CrearBecaActiva(
            DatosPrueba.CrearDatosBeca(
                estudianteId: estudianteId,
                modalidad: TipoCobertura.Total,
                permiteAcumulacion: true),
            DatosPrueba.CrearContextoOperacion());

        var parcial = DatosPrueba.CrearBecaActiva(
            DatosPrueba.CrearDatosBeca(
                estudianteId: estudianteId,
                modalidad: TipoCobertura.Parcial,
                permiteAcumulacion: true),
            DatosPrueba.CrearContextoOperacion());

        await GuardarAsync(contexto, total);
        await GuardarAsync(contexto, parcial);

        Assert.Equal(2, await contexto.Becas.CountAsync(
            beca => beca.PostulanteEstudianteId == estudianteId));
    }

    [Fact]
    public async Task Debe_PermitirBecasNoAcumulablesDelMismoPeriodo_Cuando_NoEstanActivas()
    {
        await using var contexto = _fixture.CrearContexto();
        var estudianteId = Guid.CreateVersion7();
        var periodoId = Guid.CreateVersion7();

        var primera = CrearBecaOtorgada(
            estudianteId: estudianteId,
            periodoId: periodoId,
            modalidad: TipoCobertura.Parcial,
            permiteAcumulacion: false);
        var segunda = CrearBecaOtorgada(
            estudianteId: estudianteId,
            periodoId: periodoId,
            modalidad: TipoCobertura.Parcial,
            permiteAcumulacion: false);

        await GuardarAsync(contexto, primera);
        await GuardarAsync(contexto, segunda);

        Assert.Equal(2, await contexto.Becas.CountAsync(
            beca => beca.PostulanteEstudianteId == estudianteId));
    }

    [Fact]
    public async Task Debe_ExponerLosIndicesRequeridos_Cuando_SeConsultaElEsquema()
    {
        await using var contexto = _fixture.CrearContexto();

        var indices = await contexto.Database
            .SqlQuery<string>($"""
                SELECT indexname AS "Value"
                FROM pg_indexes
                WHERE schemaname = 'becas'
                  AND indexname IN (
                      'ux_beca_estudiante_cobertura_total_activa',
                      'ux_beca_estudiante_periodo_no_acumulable_activa',
                      'ux_beca_cadena_periodo',
                      'ux_oferta_beca_beca',
                      'ux_beca_codigo')
                ORDER BY indexname
                """)
            .ToListAsync();

        Assert.Equal(
            [
                "ux_beca_cadena_periodo",
                "ux_beca_codigo",
                "ux_beca_estudiante_cobertura_total_activa",
                "ux_beca_estudiante_periodo_no_acumulable_activa",
                "ux_oferta_beca_beca"
            ],
            indices);
    }

    private static Beca CrearBecaOtorgada(
        string? codigoBeca = null,
        Guid? estudianteId = null,
        Guid? periodoId = null,
        TipoCobertura modalidad = TipoCobertura.Parcial,
        bool permiteAcumulacion = true)
        => Beca.CrearOtorgada(
            DatosPrueba.CrearDatosBeca(
                estudianteId: estudianteId,
                codigoBeca: codigoBeca,
                periodoId: periodoId,
                modalidad: modalidad,
                permiteAcumulacion: permiteAcumulacion),
            DatosPrueba.CrearContextoOperacion());

    private static async Task GuardarAsync(BecasDbContext contexto, Beca beca)
    {
        contexto.Becas.Add(beca);
        await contexto.SaveChangesAsync();
    }

    private static void AssertUniqueViolation(
        DbUpdateException excepcion,
        string nombreIndice)
    {
        var postgres = Assert.IsType<PostgresException>(excepcion.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal(nombreIndice, postgres.ConstraintName);
    }
}
