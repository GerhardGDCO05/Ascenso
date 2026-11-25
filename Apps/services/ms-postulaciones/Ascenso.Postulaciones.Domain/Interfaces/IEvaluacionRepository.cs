using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia del agregado <see cref="Evaluacion"/>.
/// </summary>
public interface IEvaluacionRepository
{
    Task<Evaluacion?> ObtenerPorAsignacionAsync(
        Guid asignacionEvaluacionId,
        CancellationToken cancellationToken = default);

    Task<Evaluacion?> ObtenerParaActualizarPorAsignacionAsync(
        Guid asignacionEvaluacionId,
        CancellationToken cancellationToken = default);

    Task<PaginaResultado<Evaluacion>> ListarPorEvaluadorAsync(
        Guid evaluadorId,
        EstadoEvaluacion? estado,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Evaluacion>> ListarDefinitivasPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        Evaluacion evaluacion,
        CancellationToken cancellationToken = default);
}
