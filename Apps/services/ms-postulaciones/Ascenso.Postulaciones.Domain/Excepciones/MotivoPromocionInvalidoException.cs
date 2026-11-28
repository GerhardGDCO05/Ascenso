namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando el motivo opcional de promoción excede la longitud máxima permitida.
/// </summary>
public sealed class MotivoPromocionInvalidoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "MOTIVO_PROMOCION_INVALIDO";

    public MotivoPromocionInvalidoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El motivo de promoción no puede superar los 500 caracteres."
                : mensaje,
            422)
    {
    }
}
