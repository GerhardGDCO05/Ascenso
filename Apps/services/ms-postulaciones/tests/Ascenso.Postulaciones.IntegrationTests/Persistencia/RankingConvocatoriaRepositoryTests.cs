using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class RankingConvocatoriaRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid convocatoriaId,
        decimal puntaje,
        DateTimeOffset fechaConsolidacion)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId: convocatoriaId,
            postulacionId: Guid.NewGuid(),
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: 5.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        return ResultadoConsolidadoRanking.Crear(
            calculo,
            fechaConsolidacion,
            Guid.NewGuid());
    }

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_LosParametrosDeLosRepositoriosDeRankingSonInvalidos()
    {
        var ctorRankingNulo = () => new RankingConvocatoriaRepository(null!);
        var ctorEstadoEvaluacionesNulo = () => new ConsultaEstadoEvaluacionesConvocatoriaRepository(null!);

        ctorRankingNulo.Should().Throw<ArgumentNullException>();
        ctorEstadoEvaluacionesNulo.Should().Throw<ArgumentNullException>();

        await using var contexto = CrearContexto();
        var repoRanking = new RankingConvocatoriaRepository(contexto);
        var repoEstadoEvaluaciones = new ConsultaEstadoEvaluacionesConvocatoriaRepository(contexto);

        var listarVacio = () => repoRanking.ListarPorConvocatoriaAsync(Guid.Empty);
        var listarParaConfirmarVacio = () => repoRanking.ListarParaConfirmarPorConvocatoriaAsync(Guid.Empty);
        var existenPendientesVacio = () => repoEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(Guid.Empty);

        await listarVacio.Should().ThrowAsync<ArgumentException>();
        await listarParaConfirmarVacio.Should().ThrowAsync<ArgumentException>();
        await existenPendientesVacio.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_ConstruirConsultasConOrdenPorPosicionYPuntajeYTrackingAdecuado_Cuando_SeInspeccionaRankingConvocatoriaRepository()
    {
        using var contexto = CrearContexto();
        var repoRanking = new RankingConvocatoriaRepository(contexto);
        var convocatoriaId = Guid.NewGuid();

        var consultaLectura = repoRanking.ConsultarPorConvocatoria(convocatoriaId);
        var sqlLectura = consultaLectura.ToQueryString();
        sqlLectura.Should().Contain("postulaciones.resultados_consolidados_ranking");
        sqlLectura.Should().Contain("convocatoria_id");
        sqlLectura.Should().Contain("ORDER BY");
        sqlLectura.Should().Contain("posicion_ranking");
        sqlLectura.Should().Contain("puntaje_consolidado");
        sqlLectura.Should().Contain("fecha_consolidacion");
        consultaLectura.Expression.ToString().Should().Contain("AsNoTracking");

        var consultaConfirmacion = repoRanking.ConsultarParaConfirmarPorConvocatoria(convocatoriaId);
        var sqlConfirmacion = consultaConfirmacion.ToQueryString();
        sqlConfirmacion.Should().Contain("postulaciones.resultados_consolidados_ranking");
        sqlConfirmacion.Should().Contain("convocatoria_id");
        sqlConfirmacion.Should().Contain("ORDER BY");
        sqlConfirmacion.Should().Contain("posicion_ranking");
        sqlConfirmacion.Should().Contain("puntaje_consolidado");
        sqlConfirmacion.Should().Contain("fecha_consolidacion");
        consultaConfirmacion.Expression.ToString().Should().NotContain("AsNoTracking");
    }

    [Fact]
    public void Debe_DetectarCambiosAlConfirmarResultadosRastreadosEnElContexto()
    {
        using var contexto = CrearContexto();
        var convocatoriaId = Guid.NewGuid();
        var resultado = CrearResultado(
            convocatoriaId,
            92.50m,
            new DateTimeOffset(2026, 11, 26, 10, 0, 0, TimeSpan.Zero));

        contexto.Attach(resultado);
        contexto.Entry(resultado).State.Should().Be(EntityState.Unchanged);

        resultado.ConfirmarRanking(
            posicionRanking: 1,
            montoEstimadoBeca: 1500.00m,
            clasificacion: ClasificacionRanking.DentroDeCupo,
            fechaConfirmacion: new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero),
            usuarioConfirmadorId: Guid.NewGuid());

        contexto.ChangeTracker.DetectChanges();
        contexto.Entry(resultado).State.Should().Be(EntityState.Modified);
    }

    [Fact]
    public void Debe_ConstruirConsultasExcluyendoReasignadasPorConflictoYDetectandoSinConsolidar_Cuando_SeInspeccionaConsultaEstadoEvaluacionesConvocatoriaRepository()
    {
        using var contexto = CrearContexto();
        var repoEstado = new ConsultaEstadoEvaluacionesConvocatoriaRepository(contexto);
        var convocatoriaId = Guid.NewGuid();

        var asignacionesPendientes = repoEstado.ConsultarAsignacionesVigentesPendientes(convocatoriaId);
        var sqlPendientes = asignacionesPendientes.ToQueryString();
        sqlPendientes.Should().Contain("postulaciones.asignaciones_evaluacion");
        sqlPendientes.Should().Contain("convocatoria_id");
        sqlPendientes.Should().Contain("REASIGNADA_POR_CONFLICTO");
        sqlPendientes.Should().Contain("COMPLETADA");
        asignacionesPendientes.Expression.ToString().Should().Contain("AsNoTracking");

        var sinConsolidar = repoEstado.ConsultarAsignacionesVigentesSinResultadoConsolidado(convocatoriaId);
        var sqlSinConsolidar = sinConsolidar.ToQueryString();
        sqlSinConsolidar.Should().Contain("postulaciones.asignaciones_evaluacion");
        sqlSinConsolidar.Should().Contain("postulaciones.resultados_consolidados_ranking");
        sqlSinConsolidar.Should().Contain("REASIGNADA_POR_CONFLICTO");
        sqlSinConsolidar.Should().Contain("postulacion_id");
        sinConsolidar.Expression.ToString().Should().Contain("AsNoTracking");
    }
}
