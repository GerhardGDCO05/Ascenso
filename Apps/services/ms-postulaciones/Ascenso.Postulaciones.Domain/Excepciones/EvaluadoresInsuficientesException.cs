namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando una postulación no alcanza el mínimo de evaluadores vigentes independientes (RN-POS-04).
/// </summary>
public sealed class EvaluadoresInsuficientesException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EVALUACIONES_INSUFICIENTES";

    public EvaluadoresInsuficientesException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La postulación requiere al menos dos evaluadores vigentes distintos."
                : mensaje,
            422)
    {
    }
}
