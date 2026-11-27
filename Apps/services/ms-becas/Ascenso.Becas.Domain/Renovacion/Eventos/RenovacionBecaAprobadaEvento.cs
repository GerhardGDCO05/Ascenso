using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.Renovacion.Eventos;

public sealed record class RenovacionBecaAprobadaEvento : DomainEvent
{
    public RenovacionBecaAprobadaEvento(Guid solicitudRenovacionId,
        Guid becaAnteriorId, Guid becaSucesoraId, Guid cadenaBecaId,
        Guid periodoOrigenId, Guid periodoNuevoId,
        Guid correlacionId, DateTimeOffset occurredAt)
        : base(correlacionId, occurredAt)
    {
        SolicitudRenovacionId = solicitudRenovacionId;
        BecaAnteriorId = becaAnteriorId;
        BecaSucesoraId = becaSucesoraId;
        CadenaBecaId = cadenaBecaId;
        PeriodoOrigenId = periodoOrigenId;
        PeriodoNuevoId = periodoNuevoId;
    }

    public Guid SolicitudRenovacionId { get; }
    public Guid BecaAnteriorId { get; }
    public Guid BecaSucesoraId { get; }
    public Guid CadenaBecaId { get; }
    public Guid PeriodoOrigenId { get; }
    public Guid PeriodoNuevoId { get; }
}
