namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta modificar o publicar una rúbrica que ya está publicada o bloqueada.
/// </summary>
public sealed class RubricaBloqueadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "RUBRICA_BLOQUEADA";

    public RubricaBloqueadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La rúbrica de evaluación no admite modificaciones ni nueva publicación en su estado actual."
                : mensaje,
            409)
    {
    }
}
