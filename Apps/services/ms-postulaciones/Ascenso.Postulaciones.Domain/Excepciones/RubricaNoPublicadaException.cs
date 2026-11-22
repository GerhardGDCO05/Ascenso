namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta bloquear una rúbrica de evaluación que aún está en borrador.
/// </summary>
public sealed class RubricaNoPublicadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "RUBRICA_NO_PUBLICADA";

    public RubricaNoPublicadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Solo una rúbrica publicada puede bloquearse; la rúbrica está en borrador."
                : mensaje,
            409)
    {
    }
}
