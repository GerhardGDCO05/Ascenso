using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.Renovacion.Eventos;

public sealed record class RenovacionBecaSolicitadaEvento : DomainEvent
{
    public RenovacionBecaSolicitadaEvento(Guid solicitudRenovacionId,
        Guid becaId, Guid periodoOrigenId, Guid periodoSolicitadoId,
        Guid correlacionId, DateTimeOffset occurredAt)
        : base(correlacionId, occurredAt)
    {
        SolicitudRenovacionId = solicitudRenovacionId;
        BecaId = becaId;
        PeriodoOrigenId = periodoOrigenId;
        PeriodoSolicitadoId = periodoSolicitadoId;
    }

    public Guid SolicitudRenovacionId { get; }
    public Guid BecaId { get; }
    public Guid PeriodoOrigenId { get; }
    public Guid PeriodoSolicitadoId { get; }
}
