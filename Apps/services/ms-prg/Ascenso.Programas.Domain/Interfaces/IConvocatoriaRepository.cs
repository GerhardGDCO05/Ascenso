using Ascenso.Programas.Domain.Convocatorias.Agregados;
using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia para el agregado de convocatorias y sus documentos requeridos.
/// </summary>
public interface IConvocatoriaRepository
{
    Task<Convocatoria?> ObtenerPorIdAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default);

    Task AgregarAsync(
        Convocatoria convocatoria,
        CancellationToken cancellationToken = default);

    void Actualizar(Convocatoria convocatoria);

    Task<(IReadOnlyList<Convocatoria> Items, int TotalCount)> ConsultarPaginadoAsync(
        int pagina,
        int tamanoPagina,
        EstadoConvocatoria? estado,
        Guid? programaBecaId,
        string? busqueda,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Convocatoria>> ConsultarAbiertasAsync(
        DateTimeOffset instanteActual,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Convocatoria>> ObtenerPublicadasVencidasAsync(
        DateTimeOffset instanteActual,
        CancellationToken cancellationToken = default);
}
