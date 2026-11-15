namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando el usuario autenticado ya posee un perfil de postulante.
/// </summary>
public sealed class PerfilYaRegistradoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "PERFIL_YA_REGISTRADO";

    public PerfilYaRegistradoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El usuario autenticado ya posee un perfil de postulante."
                : mensaje,
            409)
    {
    }
}
