namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta modificar o reenviar una evaluación ya enviada definitivamente
/// o una asignación ya completada.
/// </summary>
public sealed class EvaluacionYaEnviadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EVALUACION_YA_ENVIADA";

    public EvaluacionYaEnviadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La evaluación ya fue enviada definitivamente y no admite modificaciones ni reenvíos."
                : mensaje,
            409)
    {
    }
}
