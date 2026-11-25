namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta guardar o enviar una evaluación después de la fecha límite asignada.
/// </summary>
public sealed class PlazoEvaluacionVencidoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "PLAZO_EVALUACION_VENCIDO";

    public PlazoEvaluacionVencidoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El plazo límite para registrar o enviar la evaluación ya ha vencido."
                : mensaje,
            409)
    {
    }
}
