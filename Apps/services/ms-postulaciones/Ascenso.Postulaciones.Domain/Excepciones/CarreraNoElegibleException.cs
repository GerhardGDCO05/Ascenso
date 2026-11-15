namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la carrera informada no pertenece a la universidad o no
/// está activa.
/// </summary>
public sealed class CarreraNoElegibleException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CARRERA_NO_ELEGIBLE";

    public CarreraNoElegibleException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La carrera no pertenece a la universidad informada o no está activa."
                : mensaje)
    {
    }
}
