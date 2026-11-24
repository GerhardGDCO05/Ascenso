namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta asignar evaluaciones usando un comité evaluador inactivo.
/// </summary>
public sealed class ComiteInactivoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "COMITE_INACTIVO";

    public ComiteInactivoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El comité evaluador no se encuentra activo para asignar postulaciones."
                : mensaje,
            422)
    {
    }
}
