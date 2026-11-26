using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de solo lectura que provee las asignaciones vigentes, evaluaciones definitivas
/// y excepción de evaluador único requeridas para consolidar una postulación.
/// </summary>
public interface IConsultaDatosConsolidacionRepository
{
    Task<IReadOnlyList<AsignacionEvaluacion>> ListarAsignacionesVigentesPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Evaluacion>> ListarEvaluacionesDefinitivasPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task<AutorizacionExcepcionEvaluadorUnico?> ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);
}
