namespace Ascenso.Postulaciones.Application.Asignaciones.DTOs;

public sealed record class PaginaDto<T>(
    IReadOnlyList<T> Items,
    int Pagina,
    int Tamano,
    int Total);
