using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Domain.Universidades.Entidades;

namespace Ascenso.Programas.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia para el agregado UniversidadAliada, sus convenios, carreras y aranceles.
/// </summary>
public interface IUniversidadAliadaRepository
{
    Task<UniversidadAliada?> ObtenerPorIdAsync(Guid universidadId, CancellationToken cancellationToken = default);

    Task<bool> ExisteCodigoInstitucionalAsync(string codigoInstitucional, CancellationToken cancellationToken = default);

    Task AgregarAsync(UniversidadAliada universidad, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<UniversidadAliada> Items, int TotalCount)> ConsultarPaginadoAsync(
        int pagina,
        int tamanoPagina,
        EstadoUniversidad? estado,
        string? busqueda,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CostoMatriculaReferencia>> ConsultarHistorialCostosCarreraAsync(
        Guid universidadId,
        Guid carreraId,
        string? periodoAcademicoId,
        CancellationToken cancellationToken = default);
}
