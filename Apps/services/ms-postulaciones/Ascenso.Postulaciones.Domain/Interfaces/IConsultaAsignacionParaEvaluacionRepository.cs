using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto segregado para obtener una <see cref="AsignacionEvaluacion"/> durante el flujo de evaluación.
/// </summary>
public interface IConsultaAsignacionParaEvaluacionRepository
{
    Task<AsignacionEvaluacion?> ObtenerPorIdAsync(
        Guid asignacionEvaluacionId,
        CancellationToken cancellationToken = default);

    Task<AsignacionEvaluacion?> ObtenerParaActualizarPorIdAsync(
        Guid asignacionEvaluacionId,
        CancellationToken cancellationToken = default);
}
