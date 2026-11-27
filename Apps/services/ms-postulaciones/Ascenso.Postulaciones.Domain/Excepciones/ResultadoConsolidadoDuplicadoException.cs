namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando ya existe un resultado consolidado registrado para la postulación indicada.
/// </summary>
public sealed class ResultadoConsolidadoDuplicadoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "RESULTADO_CONSOLIDADO_DUPLICADO";

    public ResultadoConsolidadoDuplicadoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Ya existe un resultado consolidado registrado para la postulación especificada."
                : mensaje,
            409)
    {
    }
}
