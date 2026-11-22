namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la membresía solicitada no pertenece al comité evaluador.
/// </summary>
public sealed class MiembroComiteNoEncontradoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "MIEMBRO_COMITE_NO_ENCONTRADO";

    public MiembroComiteNoEncontradoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró el miembro solicitado en el comité evaluador."
                : mensaje,
            404)
    {
    }
}
