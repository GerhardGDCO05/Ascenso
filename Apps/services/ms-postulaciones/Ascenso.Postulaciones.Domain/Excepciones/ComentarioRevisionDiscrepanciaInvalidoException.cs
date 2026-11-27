namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando el comentario de revisión de discrepancia es nulo, vacío o fuera de la longitud permitida.
/// </summary>
public sealed class ComentarioRevisionDiscrepanciaInvalidoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "COMENTARIO_REVISION_REQUERIDO";

    public ComentarioRevisionDiscrepanciaInvalidoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El comentario de revisión de discrepancia es obligatorio y debe tener entre 10 y 1000 caracteres."
                : mensaje,
            422)
    {
    }
}
