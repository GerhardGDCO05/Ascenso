using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.Renovacion.Eventos;

public sealed record class BecaActivadaNuevoPeriodoEvento : DomainEvent
{
    public BecaActivadaNuevoPeriodoEvento(Guid becaId, Guid becaAnteriorId,
        Guid cadenaBecaId, Guid periodoNuevoId, Guid correlacionId,
        DateTimeOffset occurredAt)
        : base(correlacionId, occurredAt)
    {
        BecaId = becaId;
        BecaAnteriorId = becaAnteriorId;
        CadenaBecaId = cadenaBecaId;
        PeriodoNuevoId = periodoNuevoId;
    }

    public Guid BecaId { get; }
    public Guid BecaAnteriorId { get; }
    public Guid CadenaBecaId { get; }
    public Guid PeriodoNuevoId { get; }
}
