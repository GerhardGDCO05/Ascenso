namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando no existe una excepción de evaluador único autorizada para la postulación solicitada.
/// </summary>
public sealed class ExcepcionEvaluadorUnicoNoEncontradaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EXCEPCION_NO_ENCONTRADA";

    public ExcepcionEvaluadorUnicoNoEncontradaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró una excepción de evaluador único autorizada para la postulación solicitada."
                : mensaje,
            404)
    {
    }
}
