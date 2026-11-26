namespace Ascenso.Becas.Application.Renovacion.Dtos;

public sealed record PaginaRenovacionesDto(
    int Pagina, int Tamano, int Total,
    IReadOnlyList<SolicitudRenovacionDto> Elementos);
