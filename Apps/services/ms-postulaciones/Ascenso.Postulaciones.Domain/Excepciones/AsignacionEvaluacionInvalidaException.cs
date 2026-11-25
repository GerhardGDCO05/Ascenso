namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando una asignación de evaluación incumple las invariantes estructurales o temporales.
/// </summary>
public sealed class AsignacionEvaluacionInvalidaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "ASIGNACION_INVALIDA";

    public AsignacionEvaluacionInvalidaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Los datos de la asignación de evaluación no son válidos."
                : mensaje,
            422)
    {
    }
}
