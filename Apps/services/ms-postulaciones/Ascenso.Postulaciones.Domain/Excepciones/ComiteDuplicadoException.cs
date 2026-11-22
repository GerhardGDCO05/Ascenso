namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando ya existe un comité evaluador para la combinación de programa y convocatoria.
/// </summary>
public sealed class ComiteDuplicadoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "COMITE_DUPLICADO";

    public ComiteDuplicadoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Ya existe un comité evaluador para el programa y la convocatoria indicados."
                : mensaje,
            409)
    {
    }
}
