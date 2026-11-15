namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando el número de documento de identidad ya pertenece a otro
/// postulante registrado.
/// </summary>
public sealed class IdentidadDuplicadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "IDENTIDAD_DUPLICADA";

    public IdentidadDuplicadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Ya existe un postulante registrado con ese número de documento de identidad."
                : mensaje,
            409)
    {
    }
}
