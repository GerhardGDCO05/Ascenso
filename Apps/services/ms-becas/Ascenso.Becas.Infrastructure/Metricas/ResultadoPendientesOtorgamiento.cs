namespace Ascenso.Becas.Infrastructure.Metricas;

/// <summary>
/// Resultado de la consulta periódica de pendientes que alimenta los gauges
/// de negocio.
/// </summary>
public sealed record ResultadoPendientesOtorgamiento(
    long Compromisos,
    long Outbox);
