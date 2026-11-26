using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia del agregado <see cref="AsignacionEvaluacion"/>.
/// </summary>
public interface IAsignacionEvaluacionRepository
{
    Task AgregarRangoAsync(
        IReadOnlyCollection<AsignacionEvaluacion> asignaciones,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AsignacionEvaluacion>> ListarVigentesPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AsignacionEvaluacion>> ListarPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> ObtenerCargaPorMiembroAsync(
        Guid comiteEvaluadorId,
        CancellationToken cancellationToken = default);

    Task<PaginaResultado<AsignacionEvaluacion>> ListarPorEvaluadorAsync(
        Guid usuarioEvaluadorId,
        EstadoAsignacionEvaluacion? estado,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);
}
