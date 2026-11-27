namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando los parámetros o datos de ranking incumplen las invariantes del dominio.
/// </summary>
public sealed class RankingInvalidoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "RANKING_INVALIDO";

    public RankingInvalidoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Los datos proporcionados para el ranking de la convocatoria no son válidos."
                : mensaje,
            422)
    {
    }
}
