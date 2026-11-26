namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando ya existe una evaluación registrada para la asignación indicada.
/// </summary>
public sealed class EvaluacionDuplicadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EVALUACION_DUPLICADA";

    public EvaluacionDuplicadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Ya existe una evaluación registrada para la asignación especificada."
                : mensaje,
            409)
    {
    }
}
