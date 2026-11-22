namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando un usuario evaluador ya cuenta con una membresía activa en el comité.
/// </summary>
public sealed class MiembroComiteDuplicadoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "MIEMBRO_COMITE_DUPLICADO";

    public MiembroComiteDuplicadoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El evaluador ya es miembro activo del comité evaluador."
                : mensaje,
            409)
    {
    }
}
