namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

public sealed record class DesembolsosBecaSuspendidosEvento : EventoBeca
{
    public DesembolsosBecaSuspendidosEvento(Guid becaId, Guid suspensionId,
        Guid programaBecaId, Guid periodoAcademicoId, decimal saldoPendiente,
        Guid correlacionId, DateTimeOffset occurredAt)
        : base(becaId, correlacionId, occurredAt)
    {
        SuspensionId = suspensionId;
        ProgramaBecaId = programaBecaId;
        PeriodoAcademicoId = periodoAcademicoId;
        SaldoPendiente = saldoPendiente;
    }

    public Guid SuspensionId { get; }
    public Guid ProgramaBecaId { get; }
    public Guid PeriodoAcademicoId { get; }
    public decimal SaldoPendiente { get; }
}
