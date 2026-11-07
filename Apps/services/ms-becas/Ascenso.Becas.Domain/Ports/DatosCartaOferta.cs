using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Datos necesarios para generar la carta de otorgamiento.
/// </summary>
public sealed record DatosCartaOferta(
    Guid BecaId,
    Guid OfertaId,
    string NombreEstudiante,
    string NombrePrograma,
    string PeriodoCodigo,
    PoliticaCobertura Cobertura,
    decimal MontoOfertaPorPeriodo,
    DateOnly FechaVencimientoAceptacion);
