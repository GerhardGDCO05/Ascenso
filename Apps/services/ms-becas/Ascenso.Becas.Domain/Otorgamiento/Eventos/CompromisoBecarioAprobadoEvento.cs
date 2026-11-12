namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

public sealed record class CompromisoBecarioAprobadoEvento : EventoCompromisoBecario
{
    public CompromisoBecarioAprobadoEvento(
        Guid becaId,
        Guid compromisoId,
        Guid correlacionId,
        DateTimeOffset occurredAt)
        : base(becaId, compromisoId, correlacionId, occurredAt)
    {
    }
}
