namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

public sealed record class BecaRevocadaDefinitivamenteEvento : EventoBeca
{
    public BecaRevocadaDefinitivamenteEvento(Guid becaId, Guid programaBecaId,
        Guid periodoAcademicoId, Guid correlacionId, DateTimeOffset occurredAt)
        : base(becaId, correlacionId, occurredAt)
    {
        ProgramaBecaId = programaBecaId;
        PeriodoAcademicoId = periodoAcademicoId;
    }

    public Guid ProgramaBecaId { get; }
    public Guid PeriodoAcademicoId { get; }
}
