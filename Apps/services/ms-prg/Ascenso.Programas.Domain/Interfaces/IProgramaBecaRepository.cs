using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Programas.Agregados;

namespace Ascenso.Programas.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia para el agregado <see cref="ProgramaBeca"/> y sus asignaciones presupuestarias.
/// </summary>
public interface IProgramaBecaRepository
{
    Task<ProgramaBeca?> ObtenerPorIdAsync(Guid programaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default);

    Task AgregarAsync(ProgramaBeca programa, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProgramaBeca> Items, int TotalCount)> ConsultarPaginadoAsync(
        int pagina,
        int tamanoPagina,
        EstadoProgramaBeca? estado,
        string? periodoInicialId,
        ModalidadCobertura? modalidadCobertura,
        string? busqueda,
        CancellationToken cancellationToken = default);
}
