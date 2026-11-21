using Ascenso.Postulaciones.Domain.Rubricas.Agregados;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia del agregado <see cref="RubricaEvaluacion"/>.
/// </summary>
public interface IRubricaRepository
{
    Task<RubricaEvaluacion?> ObtenerPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<RubricaEvaluacion?> ObtenerParaActualizarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistePorProgramaYConvocatoriaAsync(
        Guid programaBecaId,
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        RubricaEvaluacion rubrica,
        CancellationToken cancellationToken = default);
}
