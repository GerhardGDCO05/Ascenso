namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando una evaluación o calificación incumple una invariante estructural de dominio.
/// </summary>
public sealed class EvaluacionInvalidaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EVALUACION_INVALIDA";

    public EvaluacionInvalidaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Los datos de la evaluación no son válidos."
                : mensaje,
            422)
    {
    }
}
