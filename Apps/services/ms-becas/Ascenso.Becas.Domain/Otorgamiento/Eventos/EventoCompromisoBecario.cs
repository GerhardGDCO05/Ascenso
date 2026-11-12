namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

/// <summary>
/// Base de los eventos de dominio de un compromiso becario.
/// </summary>
public abstract record class EventoCompromisoBecario : EventoBeca
{
    protected EventoCompromisoBecario(
        Guid becaId,
        Guid compromisoId,
        Guid correlacionId,
        DateTimeOffset occurredAt)
        : base(becaId, correlacionId, occurredAt)
    {
        if (compromisoId == Guid.Empty)
        {
            throw new ArgumentException("El compromiso del evento es obligatorio.", nameof(compromisoId));
        }

        CompromisoId = compromisoId;
    }

    public Guid CompromisoId { get; }
}
