namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando no existe el comité evaluador solicitado.
/// </summary>
public sealed class ComiteNoEncontradoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "COMITE_NO_ENCONTRADO";

    public ComiteNoEncontradoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró el comité evaluador solicitado."
                : mensaje,
            404)
    {
    }
}
