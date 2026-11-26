namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando existen discrepancias sin revisar en o adyacentes a la línea de corte sin acuse explícito del coordinador.
/// </summary>
public sealed class DiscrepanciasSinRevisarEnCorteException : ExcepcionReglaNegocio
{
    public const string CodigoError = "DISCREPANCIAS_SIN_REVISAR_EN_CORTE";

    public DiscrepanciasSinRevisarEnCorteException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Existen alertas de discrepancia sin revisar dentro o adyacentes a la línea de corte; requiere acuse explícito para confirmar el ranking."
                : mensaje,
            422)
    {
    }
}
