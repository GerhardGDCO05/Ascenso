namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando el usuario decisor tiene un conflicto de interés declarado sobre el grupo afectado por el caso límite (RN-POS-06).
/// </summary>
public sealed class ConflictoInteresDecisorCasoLimiteException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CONFLICTO_INTERES_DECISOR";

    public ConflictoInteresDecisorCasoLimiteException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El usuario decisor presenta un conflicto de interés declarado sobre las postulaciones afectadas por el caso límite."
                : mensaje,
            403)
    {
    }
}
