namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de lectura que verifica si una convocatoria aún posee evaluaciones pendientes o postulaciones sin consolidar.
/// </summary>
public interface IConsultaEstadoEvaluacionesConvocatoriaRepository
{
    Task<bool> ExistenEvaluacionesPendientesOSinConsolidarAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);
}
