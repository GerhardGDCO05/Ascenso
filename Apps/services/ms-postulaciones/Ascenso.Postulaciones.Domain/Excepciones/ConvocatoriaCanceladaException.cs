namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta operar sobre evaluaciones de una convocatoria cancelada.
/// </summary>
public sealed class ConvocatoriaCanceladaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CONVOCATORIA_CANCELADA";

    public ConvocatoriaCanceladaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La convocatoria asociada se encuentra cancelada y no admite operaciones de evaluación."
                : mensaje,
            409)
    {
    }
}
