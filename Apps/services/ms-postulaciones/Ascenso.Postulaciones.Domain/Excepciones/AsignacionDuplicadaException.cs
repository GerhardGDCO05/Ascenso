namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando un evaluador ya cuenta con una asignación vigente para la misma postulación.
/// </summary>
public sealed class AsignacionDuplicadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "ASIGNACION_DUPLICADA";

    public AsignacionDuplicadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El evaluador ya tiene una asignación vigente para esta postulación."
                : mensaje,
            409)
    {
    }
}
