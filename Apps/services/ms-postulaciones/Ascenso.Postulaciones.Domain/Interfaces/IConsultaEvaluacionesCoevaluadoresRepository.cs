using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de solo lectura para consultar asignaciones vigentes y evaluaciones definitivas de coevaluadores.
/// </summary>
public interface IConsultaEvaluacionesCoevaluadoresRepository
{
    Task<AsignacionEvaluacion?> ObtenerAsignacionVigenteEvaluadorAsync(
        Guid postulacionId,
        Guid usuarioEvaluadorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Evaluacion>> ListarDefinitivasDeCoevaluadoresAsync(
        Guid postulacionId,
        Guid usuarioEvaluadorId,
        CancellationToken cancellationToken = default);
}
