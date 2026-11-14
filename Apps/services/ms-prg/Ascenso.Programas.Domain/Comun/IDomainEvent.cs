namespace Ascenso.Programas.Domain.Comun;

/// <summary>
/// Contrato mínimo de un evento de dominio.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }

    Guid CorrelationId { get; }
}

