namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de verificación de conflictos de interés del usuario decisor sobre postulaciones afectadas
/// por un caso límite de ranking (RN-POS-06, punto de extensión OCP/ISP para RF-39).
/// </summary>
public interface IVerificadorConflictoInteresDecisor
{
    Task<bool> TieneConflictoEnPostulacionesAsync(
        Guid convocatoriaId,
        Guid usuarioDecisorId,
        IReadOnlyCollection<Guid> postulacionIds,
        CancellationToken cancellationToken = default);
}
