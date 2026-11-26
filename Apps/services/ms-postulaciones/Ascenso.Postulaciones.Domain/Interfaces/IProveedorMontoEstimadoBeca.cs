namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto ACL desacoplado para resolver en lote el monto estimado de beca por postulación.
/// </summary>
public interface IProveedorMontoEstimadoBeca
{
    Task<IReadOnlyDictionary<Guid, decimal>> ObtenerMontosEstimadosPorPostulacionAsync(
        Guid convocatoriaId,
        Guid programaBecaId,
        IReadOnlyCollection<Guid> postulacionIds,
        CancellationToken cancellationToken = default);
}
