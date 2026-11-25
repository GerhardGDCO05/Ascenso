namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando no existe una evaluación asociada a la asignación solicitada.
/// </summary>
public sealed class EvaluacionNoEncontradaException : ExcepcionEntidadNoEncontrada
{
    public new const string CodigoError = "EVALUACION_NO_ENCONTRADA";

    public EvaluacionNoEncontradaException(string? mensaje = null)
        : base(
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró una evaluación registrada para la asignación solicitada."
                : mensaje,
            CodigoError)
    {
    }
}
