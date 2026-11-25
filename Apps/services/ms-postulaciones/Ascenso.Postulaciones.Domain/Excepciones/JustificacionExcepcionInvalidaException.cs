namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la justificación de una excepción de evaluador único es ausente o incumple la longitud requerida (RN-AUD-01).
/// </summary>
public sealed class JustificacionExcepcionInvalidaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "JUSTIFICACION_REQUERIDA";

    public JustificacionExcepcionInvalidaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La justificación de la excepción de evaluador único es obligatoria y debe tener entre 10 y 1000 caracteres."
                : mensaje,
            400)
    {
    }
}
