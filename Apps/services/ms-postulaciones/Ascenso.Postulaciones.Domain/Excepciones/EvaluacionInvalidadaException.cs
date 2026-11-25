namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta modificar o enviar una evaluación invalidada por conflicto de interés.
/// </summary>
public sealed class EvaluacionInvalidadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EVALUACION_INVALIDADA";

    public EvaluacionInvalidadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La evaluación fue invalidada por conflicto de interés y no puede modificarse ni enviarse."
                : mensaje,
            409)
    {
    }
}
