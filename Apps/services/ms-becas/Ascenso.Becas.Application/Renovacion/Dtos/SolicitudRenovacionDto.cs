using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Renovacion.Agregados;

namespace Ascenso.Becas.Application.Renovacion.Dtos;

public sealed record SolicitudRenovacionDto(
    Guid Id, Guid BecaId, Guid PeriodoOrigenId, Guid PeriodoSolicitadoId,
    DateOnly FechaLimiteVentanaRenovacion, EstadoRenovacion Estado,
    decimal MontoReservadoRenovacion, Guid? ReservaPresupuestoId,
    string? MotivoDecision, DateTimeOffset? FechaDecision)
{
    public static SolicitudRenovacionDto Desde(SolicitudRenovacion solicitud)
        => new(solicitud.Id, solicitud.BecaId, solicitud.PeriodoOrigenId,
            solicitud.PeriodoSolicitadoId, solicitud.FechaLimiteVentanaRenovacion,
            solicitud.Estado, solicitud.MontoReservadoRenovacion,
            solicitud.ReservaPresupuestoId, solicitud.MotivoDecision,
            solicitud.FechaDecision);
}
