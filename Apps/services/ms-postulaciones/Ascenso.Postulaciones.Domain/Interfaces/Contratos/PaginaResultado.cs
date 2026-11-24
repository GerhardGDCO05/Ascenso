namespace Ascenso.Postulaciones.Domain.Interfaces.Contratos;

/// <summary>
/// Resultado paginado de lectura devuelto por los puertos de repositorio.
/// </summary>
/// <typeparam name="T">Tipo de los elementos incluidos en la página.</typeparam>
public sealed record class PaginaResultado<T>(
    IReadOnlyList<T> Items,
    int Total);
