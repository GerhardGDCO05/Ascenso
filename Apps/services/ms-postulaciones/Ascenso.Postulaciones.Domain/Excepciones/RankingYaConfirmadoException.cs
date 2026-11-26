namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta modificar, recalcular o revisar un resultado consolidado cuyo ranking ya fue confirmado.
/// </summary>
public sealed class RankingYaConfirmadoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "RANKING_YA_CONFIRMADO";

    public RankingYaConfirmadoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El resultado consolidado pertenece a un ranking ya confirmado y no admite modificaciones."
                : mensaje,
            409)
    {
    }
}
