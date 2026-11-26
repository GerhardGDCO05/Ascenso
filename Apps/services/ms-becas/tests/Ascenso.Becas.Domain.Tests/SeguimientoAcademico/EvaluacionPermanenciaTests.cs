using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Tests.SeguimientoAcademico;

public sealed class EvaluacionPermanenciaTests
{
    [Fact]
    public void CumplimientoTotalNoRecomiendaConsecuencia()
    {
        var reporte = CrearReporte(17m, 5, 0, 20);

        var evaluacion = reporte.Evaluar(CrearCondiciones(), CrearContexto());

        Assert.True(evaluacion.CumpleCondiciones);
        Assert.Null(evaluacion.ConsecuenciaRecomendada);
        Assert.Equal(4, evaluacion.DetalleCondiciones.Count);
        Assert.All(evaluacion.DetalleCondiciones, detalle => Assert.True(detalle.Cumple));
    }

    [Fact]
    public void CumplimientoParcialRegistraCondicionesIncumplidas()
    {
        var reporte = CrearReporte(16m, 2, 0, 20);

        var evaluacion = reporte.Evaluar(CrearCondiciones(), CrearContexto());

        Assert.False(evaluacion.CumpleCondiciones);
        Assert.Equal(ConsecuenciaPermanencia.Suspension, evaluacion.ConsecuenciaRecomendada);
        Assert.Equal(2, evaluacion.DetalleCondiciones.Count(detalle => !detalle.Cumple));
        Assert.Null(evaluacion.ConsecuenciaAplicada);
    }

    [Fact]
    public void IncumplimientoTotalRecomiendaSinAplicarHastaDecision()
    {
        var reporte = CrearReporte(10m, 0, 3, 0);
        var coordinador = CrearContexto();

        var evaluacion = reporte.Evaluar(CrearCondiciones(), CrearContexto());
        Assert.False(evaluacion.CumpleCondiciones);
        Assert.All(evaluacion.DetalleCondiciones, detalle => Assert.False(detalle.Cumple));
        Assert.Equal(EstadoConsecuencia.PendienteConfirmacion, evaluacion.EstadoConfirmacion);

        reporte.DecidirConsecuencia(true, "Incumplimiento verificado",
            "Presentar un plan de subsanación", coordinador);

        Assert.Null(evaluacion.ConsecuenciaAplicada);
        Assert.Equal(EstadoConsecuencia.Confirmada, evaluacion.EstadoConfirmacion);
        Assert.Equal(coordinador.UsuarioAutorId, evaluacion.EvaluadoPorCoordinadorId);
        reporte.MarcarConsecuenciaAplicada(coordinador);
        Assert.Equal(ConsecuenciaPermanencia.Suspension, evaluacion.ConsecuenciaAplicada);
        Assert.Throws<InvalidOperationException>(() => reporte.DecidirConsecuencia(false,
            "Otra decisión", "Otro paso", coordinador));
    }

    [Fact]
    public void SinEvidenciaDeHorasRequeridasNoEvalua()
    {
        var reporte = CrearReporte(17m, 5, 0, null);

        Assert.Throws<InvalidOperationException>(() =>
            reporte.Evaluar(CrearCondiciones(), CrearContexto()));
        Assert.Null(reporte.Evaluacion);
    }

    [Fact]
    public void HorasPositivasSinComprobanteNoCreanReporte()
    {
        Assert.Throws<ArgumentException>(() => CrearReporte(17m, 5, 0, 20,
            incluirComprobanteServicio: false));
    }

    [Fact]
    public void CoordinadorPuedeRechazarConsecuenciaRecomendada()
    {
        var reporte = CrearReporte(10m, 5, 0, 20);
        var evaluacion = reporte.Evaluar(CrearCondiciones(), CrearContexto());

        reporte.DecidirConsecuencia(false, "Se requiere aclarar el promedio",
            "Aportar acta de notas corregida", CrearContexto());

        Assert.Equal(EstadoConsecuencia.Rechazada, evaluacion.EstadoConfirmacion);
        Assert.Null(evaluacion.ConsecuenciaAplicada);
    }

    [Fact]
    public void NoApruebaReporteSinEvaluacionConfirmada()
    {
        var reporte = CrearReporte(17m, 5, 0, 20);
        var coordinador = CrearContexto();
        Assert.Throws<InvalidOperationException>(() => reporte.ResolverRevision(true, coordinador));

        reporte.Evaluar(CrearCondiciones(), coordinador);
        Assert.Throws<InvalidOperationException>(() => reporte.ResolverRevision(true, coordinador));
        reporte.DecidirConsecuencia(true, "Cumple la política",
            "Continuar con el siguiente periodo", coordinador);
        reporte.ResolverRevision(true, coordinador);
        Assert.Equal(EstadoReporteAcademico.Aprobado, reporte.Estado);
    }

    private static ReporteAcademico CrearReporte(decimal promedio,
        int aprobadas, int reprobadas, int? horas, bool incluirComprobanteServicio = true)
    {
        var datos = new DatosReporteAcademico(Guid.CreateVersion7(), Guid.CreateVersion7(),
            promedio, aprobadas, reprobadas, 0, aprobadas * 4,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", FuenteReporte.Becario,
            Guid.CreateVersion7(), new DateOnly(2026, 11, 25), horas,
            incluirComprobanteServicio ? "gs://becas/servicio/constancia.pdf" : null,
            incluirComprobanteServicio ? new string('b', 64) : null);
        var reporte = ReporteAcademico.Registrar(datos, CrearContexto());
        reporte.IniciarRevision(CrearContexto());
        return reporte;
    }

    private static CondicionesPermanencia CrearCondiciones()
        => new(17m, 5, 1, 20, ConsecuenciaPermanencia.Suspension);

    private static ContextoOperacion CrearContexto()
        => new(Guid.CreateVersion7(), Guid.CreateVersion7(),
            new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero));
}
