namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta sobrescribir una decisión de caso límite ya registrada o recalcular un resultado con decisión inalterable.
/// </summary>
public sealed class CasoLimiteYaResueltoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CASO_LIMITE_YA_RESUELTO";

    public CasoLimiteYaResueltoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La decisión de desempate o caso límite ya fue registrada y es inalterable."
                : mensaje,
            409)
    {
    }
}
