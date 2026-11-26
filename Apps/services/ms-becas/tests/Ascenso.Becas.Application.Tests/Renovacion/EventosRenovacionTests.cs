using System.Text.Json;
using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using Ascenso.Becas.Domain.Renovacion.Eventos;

namespace Ascenso.Becas.Application.Tests.Renovacion;

public sealed class EventosRenovacionTests
{
    [Fact]
    public void MapeaSolicitudYConclusionSinDatosPersonales()
    {
        var becaId = Guid.CreateVersion7();
        var solicitudId = Guid.CreateVersion7();
        var origenId = Guid.CreateVersion7();
        var destinoId = Guid.CreateVersion7();
        var correlacionId = Guid.CreateVersion7();
        var fecha = DateTimeOffset.UtcNow;

        var solicitud = new RenovacionBecaSolicitadaEvento(solicitudId,
            becaId, origenId, destinoId, correlacionId, fecha);
        var conclusion = new BecaConcluidaPorVencimientoRenovacionEvento(
            becaId, Guid.CreateVersion7(), Guid.CreateVersion7(), origenId,
            destinoId, correlacionId, fecha);
        var mensajeSolicitud = Assert.IsType<EventoIntegracion<RenovacionBecaSolicitada>>(
            MapeadorEventosIntegracion.Mapear(solicitud, 1));
        var mensajeConclusion = Assert.IsType<
            EventoIntegracion<BecaConcluidaPorVencimientoRenovacion>>(
            MapeadorEventosIntegracion.Mapear(conclusion, 2));

        Assert.Equal(solicitudId, mensajeSolicitud.AggregateId);
        Assert.Equal(becaId, mensajeSolicitud.Payload.BecaId);
        Assert.Equal(becaId, mensajeConclusion.AggregateId);
        Assert.Equal(destinoId, mensajeConclusion.Payload.PeriodoSolicitadoId);
        var json = JsonSerializer.Serialize(new object[]
        {
            mensajeSolicitud, mensajeConclusion
        });
        Assert.DoesNotContain("documento", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postulante", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("promedio", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("monto", json, StringComparison.OrdinalIgnoreCase);
    }
}
