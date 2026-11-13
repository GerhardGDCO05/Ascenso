namespace Ascenso.Programas.Application.Donantes.DTOs;

public sealed record class PaginadoResponse<T>(
    IReadOnlyList<T> Items,
    int Pagina,
    int TamanoPagina,
    int TotalRegistros);

