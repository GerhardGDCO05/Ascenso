using Ascenso.Postulaciones.Domain.Interfaces;

namespace Ascenso.Postulaciones.Infrastructure.Adapters;

/// <summary>
/// Adaptador por defecto sin operación para la verificación de conflictos de interés del usuario decisor
/// hasta la implementación de RF-39 (<c>ConflictoInteres</c>).
/// </summary>
public sealed class VerificadorConflictoInteresDecisorSinOperacion : IVerificadorConflictoInteresDecisor
{
    public Task<bool> TieneConflictoEnPostulacionesAsync(
        Guid convocatoriaId,
        Guid usuarioDecisorId,
        IReadOnlyCollection<Guid> postulacionIds,
        CancellationToken cancellationToken = default)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }

        if (usuarioDecisorId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario decisor es obligatorio.",
                nameof(usuarioDecisorId));
        }

        ArgumentNullException.ThrowIfNull(postulacionIds);

        if (postulacionIds.Count == 0 || postulacionIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException(
                "La colección de postulaciones a verificar no puede estar vacía ni contener identificadores vacíos.",
                nameof(postulacionIds));
        }

        return Task.FromResult(false);
    }
}
