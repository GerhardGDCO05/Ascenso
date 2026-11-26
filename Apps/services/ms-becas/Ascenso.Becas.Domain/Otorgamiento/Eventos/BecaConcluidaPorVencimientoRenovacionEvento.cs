using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

public sealed record class BecaConcluidaPorVencimientoRenovacionEvento : DomainEvent
{
    public BecaConcluidaPorVencimientoRenovacionEvento(Guid becaId,
        Guid cadenaBecaId, Guid programaBecaId, Guid periodoOrigenId,
        Guid periodoSolicitadoId, Guid correlacionId, DateTimeOffset occurredAt)
        : base(correlacionId, occurredAt)
    {
        BecaId = becaId;
        CadenaBecaId = cadenaBecaId;
        ProgramaBecaId = programaBecaId;
        PeriodoOrigenId = periodoOrigenId;
        PeriodoSolicitadoId = periodoSolicitadoId;
    }

    public Guid BecaId { get; }
    public Guid CadenaBecaId { get; }
    public Guid ProgramaBecaId { get; }
    public Guid PeriodoOrigenId { get; }
    public Guid PeriodoSolicitadoId { get; }
}
