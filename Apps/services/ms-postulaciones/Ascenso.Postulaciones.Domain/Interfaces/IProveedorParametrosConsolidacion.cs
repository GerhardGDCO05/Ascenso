using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto para resolver los parámetros vigentes de consolidación (evaluadores mínimos requeridos
/// y umbral de discrepancia) desacoplando su origen por OCP.
/// </summary>
public interface IProveedorParametrosConsolidacion
{
    Task<ParametrosConsolidacion> ObtenerParametrosAsync(
        Guid convocatoriaId,
        Guid rubricaEvaluacionId,
        CancellationToken cancellationToken = default);
}
