namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando no existe un resultado consolidado registrado para la postulación solicitada.
/// </summary>
public sealed class ResultadoConsolidadoNoEncontradoException : ExcepcionEntidadNoEncontrada
{
    public new const string CodigoError = "RESULTADO_CONSOLIDADO_NO_ENCONTRADO";

    public ResultadoConsolidadoNoEncontradoException(string? mensaje = null)
        : base(
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró un resultado consolidado para la postulación solicitada."
                : mensaje,
            CodigoError)
    {
    }
}
