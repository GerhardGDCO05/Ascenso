using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.SeguimientoAcademico.Eventos;

namespace Ascenso.Becas.Application.Tests.SeguimientoAcademico;

public sealed class EventosSeguimientoAcademicoTests
{
    [Fact]
    public void MapeaEntregaYAlertaTardiaSinCalificacionesNiDatosPersonales()
    {
        var reporteId = Guid.CreateVersion7();
        var becaId = Guid.CreateVersion7();
        var periodoId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var correlacionId = Guid.CreateVersion7();
        var fecha = DateTimeOffset.UtcNow;
        var limite = DateOnly.FromDateTime(fecha.UtcDateTime.AddDays(-1));

        var entrega = new ReporteAcademicoEntregadoEvento(reporteId, becaId,
            periodoId, limite, true, correlacionId, fecha);
        var alerta = new AlertaReporteAcademicoTardioEvento(reporteId, becaId,
            programaId, periodoId, limite, correlacionId, fecha);
        var mensajeEntrega = Assert.IsType<EventoIntegracion<ReporteAcademicoEntregado>>(
            MapeadorEventosIntegracion.Mapear(entrega, 1));
        var mensajeAlerta = Assert.IsType<EventoIntegracion<AlertaReporteAcademicoTardio>>(
            MapeadorEventosIntegracion.Mapear(alerta, 1));

        Assert.True(mensajeEntrega.Payload.EntregaTardia);
        Assert.Equal(reporteId, mensajeEntrega.AggregateId);
        Assert.Equal(programaId, mensajeAlerta.Payload.ProgramaBecaId);
        Assert.DoesNotContain("promedio", System.Text.Json.JsonSerializer
            .Serialize(mensajeEntrega), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MapeaEvaluacionYConsecuenciaSinDatosAcademicosNiSocioeconomicos()
    {
        var evaluacionId = Guid.CreateVersion7();
        var reporteId = Guid.CreateVersion7();
        var becaId = Guid.CreateVersion7();
        var correlacionId = Guid.CreateVersion7();
        var fecha = DateTimeOffset.UtcNow;

        var evaluacion = new EvaluacionPermanenciaRegistradaEvento(
            evaluacionId, reporteId, becaId, false,
            ConsecuenciaPermanencia.LlamadoAtencion, correlacionId, fecha);
        var consecuencia = new ConsecuenciaPermanenciaAplicadaEvento(
            evaluacionId, reporteId, becaId,
            ConsecuenciaPermanencia.LlamadoAtencion, correlacionId, fecha);

        var mensajeEvaluacion = Assert.IsType<EventoIntegracion<EvaluacionPermanenciaRegistrada>>(
            MapeadorEventosIntegracion.Mapear(evaluacion, 2));
        var mensajeConsecuencia = Assert.IsType<EventoIntegracion<ConsecuenciaPermanenciaAplicada>>(
            MapeadorEventosIntegracion.Mapear(consecuencia, 3));

        Assert.Equal(reporteId, mensajeEvaluacion.AggregateId);
        Assert.Equal(2, mensajeEvaluacion.AggregateVersion);
        Assert.Equal("LLAMADO_ATENCION", mensajeEvaluacion.Payload.ConsecuenciaRecomendada);
        Assert.Equal("LLAMADO_ATENCION", mensajeConsecuencia.Payload.ConsecuenciaAplicada);
        Assert.Equal(3, mensajeConsecuencia.AggregateVersion);
        var json = System.Text.Json.JsonSerializer.Serialize(new object[]
        {
            mensajeEvaluacion, mensajeConsecuencia
        });
        Assert.DoesNotContain("promedio", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("monto", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("documento", json, StringComparison.OrdinalIgnoreCase);
    }
}
