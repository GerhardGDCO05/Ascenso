namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando no existe la rúbrica de evaluación solicitada.
/// </summary>
public sealed class RubricaNoEncontradaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "RUBRICA_NO_ENCONTRADA";

    public RubricaNoEncontradaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró la rúbrica de evaluación solicitada."
                : mensaje,
            404)
    {
    }
}
