using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

/// <summary>
/// Base de los eventos de dominio que identifican una beca.
/// </summary>
public abstract record class EventoBeca : DomainEvent
{
    protected EventoBeca(
        Guid becaId,
        Guid correlacionId,
        DateTimeOffset occurredAt)
        : base(correlacionId, occurredAt)
    {
        if (becaId == Guid.Empty)
        {
            throw new ArgumentException("La beca del evento es obligatoria.", nameof(becaId));
        }

        BecaId = becaId;
    }

    public Guid BecaId { get; }
}
