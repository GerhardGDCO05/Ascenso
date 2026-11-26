namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando los parámetros o datos de entrada para la consolidación incumplen las invariantes del dominio.
/// </summary>
public sealed class ConsolidacionInvalidaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CONSOLIDACION_INVALIDA";

    public ConsolidacionInvalidaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Los datos proporcionados para la consolidación no son válidos."
                : mensaje,
            422)
    {
    }
}
