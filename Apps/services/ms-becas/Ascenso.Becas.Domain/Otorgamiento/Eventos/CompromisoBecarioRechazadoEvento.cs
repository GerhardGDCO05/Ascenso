namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

public sealed record class CompromisoBecarioRechazadoEvento : EventoCompromisoBecario
{
    public CompromisoBecarioRechazadoEvento(
        Guid becaId,
        Guid compromisoId,
        Guid correlacionId,
        DateTimeOffset occurredAt)
        : base(becaId, compromisoId, correlacionId, occurredAt)
    {
    }
}
