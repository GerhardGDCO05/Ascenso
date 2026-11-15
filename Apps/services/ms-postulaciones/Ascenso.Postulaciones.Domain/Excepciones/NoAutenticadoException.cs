namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la operación requiere un usuario autenticado y no lo hay.
/// </summary>
public sealed class NoAutenticadoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "NO_AUTENTICADO";

    public NoAutenticadoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La operación requiere un usuario autenticado."
                : mensaje,
            401)
    {
    }
}
