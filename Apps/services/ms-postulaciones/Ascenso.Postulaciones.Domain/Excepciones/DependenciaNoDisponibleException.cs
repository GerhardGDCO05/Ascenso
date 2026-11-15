namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando una dependencia externa indispensable no responde y la
/// operación debe fallar cerrada.
/// </summary>
public sealed class DependenciaNoDisponibleException : ExcepcionReglaNegocio
{
    public const string CodigoError = "DEPENDENCIA_NO_DISPONIBLE";

    public DependenciaNoDisponibleException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Una dependencia externa indispensable no se encuentra disponible."
                : mensaje,
            503)
    {
    }
}
