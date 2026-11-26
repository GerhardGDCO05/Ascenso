namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta revisar una discrepancia sobre un resultado consolidado que no tiene alerta activa.
/// </summary>
public sealed class DiscrepanciaNoDetectadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "DISCREPANCIA_NO_DETECTADA";

    public DiscrepanciaNoDetectadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El resultado consolidado no presenta una alerta de discrepancia activa para revisar."
                : mensaje,
            422)
    {
    }
}
