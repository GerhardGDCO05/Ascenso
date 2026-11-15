namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Resultado de aceptar una oferta de beca.
/// </summary>
public sealed record OfertaRespondidaDto(
    Guid OfertaId,
    Guid BecaId,
    string EstadoOferta,
    string EstadoBeca);
