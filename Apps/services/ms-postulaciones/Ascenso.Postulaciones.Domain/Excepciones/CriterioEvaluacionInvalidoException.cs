namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando los datos de un criterio de evaluación no son válidos.
/// </summary>
public sealed class CriterioEvaluacionInvalidoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CRITERIO_INVALIDO";

    public CriterioEvaluacionInvalidoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Los datos del criterio de evaluación no son válidos."
                : mensaje,
            400)
    {
    }
}
