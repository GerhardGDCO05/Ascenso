namespace Ascenso.Postulaciones.Domain.Comun;

/// <summary>
/// Base inmutable para eventos de dominio.
/// </summary>
public abstract record class DomainEvent : IDomainEvent
{
    protected DomainEvent(Guid correlationId, DateTimeOffset occurredAt, Guid? eventId = null)
    {
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("La correlación del evento es obligatoria.", nameof(correlationId));
        }

        if (occurredAt == default)
        {
            throw new ArgumentException("La fecha de ocurrencia del evento es obligatoria.", nameof(occurredAt));
        }

        EventId = eventId ?? UuidV7.Nuevo();
        OccurredAt = occurredAt.ToUniversalTime();
        CorrelationId = correlationId;
    }

    public Guid EventId { get; }

    public DateTimeOffset OccurredAt { get; }

    public Guid CorrelationId { get; }
}

