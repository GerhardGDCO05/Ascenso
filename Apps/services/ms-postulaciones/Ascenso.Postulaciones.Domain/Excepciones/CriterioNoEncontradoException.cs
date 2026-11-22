namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando no existe el criterio de evaluación solicitado dentro de la rúbrica.
/// </summary>
public sealed class CriterioNoEncontradoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CRITERIO_NO_ENCONTRADO";

    public CriterioNoEncontradoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró el criterio de evaluación solicitado."
                : mensaje,
            404)
    {
    }
}
