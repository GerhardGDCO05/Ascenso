namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Resultado de emitir una oferta de beca.
/// </summary>
public sealed record OfertaEmitidaDto(
    Guid OfertaId,
    Guid BecaId,
    string Estado,
    DateTimeOffset FechaVencimientoAceptacion);
