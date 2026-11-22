using Ascenso.Postulaciones.Domain.Comites.Agregados;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia del agregado <see cref="ComiteEvaluador"/>.
/// </summary>
public interface IComiteEvaluadorRepository
{
    Task<ComiteEvaluador?> ObtenerPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ComiteEvaluador?> ObtenerParaActualizarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ComiteEvaluador?> ObtenerPorProgramaYConvocatoriaAsync(
        Guid programaBecaId,
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistePorProgramaYConvocatoriaAsync(
        Guid programaBecaId,
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        ComiteEvaluador comite,
        CancellationToken cancellationToken = default);
}
