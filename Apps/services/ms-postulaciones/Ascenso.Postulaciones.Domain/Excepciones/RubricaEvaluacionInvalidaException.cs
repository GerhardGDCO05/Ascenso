namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando los datos de una rúbrica de evaluación no son válidos.
/// </summary>
public sealed class RubricaEvaluacionInvalidaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "RUBRICA_INVALIDA";

    public RubricaEvaluacionInvalidaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Los datos de la rúbrica de evaluación no son válidos."
                : mensaje,
            400)
    {
    }
}
