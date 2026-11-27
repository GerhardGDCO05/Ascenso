namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la justificación obligatoria de resolución de un caso límite está vacía o fuera del rango permitido.
/// </summary>
public sealed class JustificacionCasoLimiteInvalidaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "JUSTIFICACION_CASO_LIMITE_REQUERIDA";

    public JustificacionCasoLimiteInvalidaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La justificación de resolución del caso límite es obligatoria y debe tener entre 10 y 1000 caracteres."
                : mensaje,
            422)
    {
    }
}
