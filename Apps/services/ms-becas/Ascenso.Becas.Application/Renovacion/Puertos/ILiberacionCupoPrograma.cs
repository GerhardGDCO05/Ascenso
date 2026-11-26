namespace Ascenso.Becas.Application.Renovacion.Puertos;

/// <summary>Libera en MS-PRG un cupo ocupado al concluir una beca.</summary>
public interface ILiberacionCupoPrograma
{
    /// <summary>
    /// La operación debe ser idempotente por beca y periodo. Devuelve null
    /// cuando MS-PRG no puede confirmar la liberación.
    /// </summary>
    Task<bool?> LiberarPorConclusionAsync(
        Guid programaBecaId, Guid becaId, Guid periodoVigenteId,
        CancellationToken cancellationToken = default);
}
