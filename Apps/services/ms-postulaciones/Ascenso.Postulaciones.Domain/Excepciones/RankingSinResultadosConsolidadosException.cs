namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta confirmar el ranking de una convocatoria que no posee resultados consolidados.
/// </summary>
public sealed class RankingSinResultadosConsolidadosException : ExcepcionReglaNegocio
{
    public const string CodigoError = "RANKING_SIN_RESULTADOS";

    public RankingSinResultadosConsolidadosException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No existen resultados consolidados para confirmar el ranking de la convocatoria."
                : mensaje,
            422)
    {
    }
}
