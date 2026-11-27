namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta sobrescribir la revisión de una alerta de discrepancia ya revisada.
/// </summary>
public sealed class DiscrepanciaYaRevisadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "DISCREPANCIA_YA_REVISADA";

    public DiscrepanciaYaRevisadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La alerta de discrepancia de este resultado consolidado ya fue revisada previamente."
                : mensaje,
            409)
    {
    }
}
