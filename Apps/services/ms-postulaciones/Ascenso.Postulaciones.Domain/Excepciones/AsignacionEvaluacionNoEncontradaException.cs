namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando no existe la asignación de evaluación solicitada.
/// </summary>
public sealed class AsignacionEvaluacionNoEncontradaException : ExcepcionEntidadNoEncontrada
{
    public new const string CodigoError = "ASIGNACION_NO_ENCONTRADA";

    public AsignacionEvaluacionNoEncontradaException(string? mensaje = null)
        : base(
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró la asignación de evaluación solicitada."
                : mensaje,
            CodigoError)
    {
    }
}
