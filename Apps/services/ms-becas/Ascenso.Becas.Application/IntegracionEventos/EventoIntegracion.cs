using System.Text.Json.Serialization;

namespace Ascenso.Becas.Application.IntegracionEventos;

/// <summary>
/// Sobre común de todos los eventos de integración publicados por MS-BEC.
/// No contiene datos personales: solo identificadores, estados y metadatos
/// técnicos necesarios para la idempotencia y la trazabilidad.
/// </summary>
public sealed record class EventoIntegracion<T>
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; }

    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; init; }

    [JsonPropertyName("producer")]
    public string Producer { get; init; } = string.Empty;

    [JsonPropertyName("aggregateId")]
    public Guid AggregateId { get; init; }

    [JsonPropertyName("aggregateVersion")]
    public int AggregateVersion { get; init; }

    [JsonPropertyName("payload")]
    public T Payload { get; init; } = default!;
}
