namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta calificar un criterio que no pertenece a la rúbrica de la convocatoria.
/// </summary>
public sealed class CriterioNoPerteneceARubricaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CRITERIO_NO_PERTENECE_A_RUBRICA";

    public CriterioNoPerteneceARubricaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El criterio indicado no pertenece a la rúbrica de evaluación de la convocatoria."
                : mensaje,
            422)
    {
    }
}
