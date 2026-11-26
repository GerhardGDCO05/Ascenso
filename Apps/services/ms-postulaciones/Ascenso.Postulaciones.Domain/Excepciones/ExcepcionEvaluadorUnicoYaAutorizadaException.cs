namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando ya existe una excepción de evaluador único autorizada para la postulación.
/// </summary>
public sealed class ExcepcionEvaluadorUnicoYaAutorizadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EXCEPCION_YA_AUTORIZADA";

    public ExcepcionEvaluadorUnicoYaAutorizadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La postulación ya cuenta con una excepción de evaluador único autorizada."
                : mensaje,
            409)
    {
    }
}
