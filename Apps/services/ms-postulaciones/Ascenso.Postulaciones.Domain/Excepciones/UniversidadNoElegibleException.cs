namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la universidad informada no es elegible para el registro
/// del postulante.
/// </summary>
public sealed class UniversidadNoElegibleException : ExcepcionReglaNegocio
{
    public const string CodigoError = "UNIVERSIDAD_NO_ELEGIBLE";

    public UniversidadNoElegibleException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La universidad no existe, no está activa o no tiene convenio vigente."
                : mensaje)
    {
    }
}
