namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta consultar evaluaciones de coevaluadores antes del cierre formal del periodo de revisión.
/// </summary>
public sealed class PeriodoRevisionAbiertoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "PERIODO_REVISION_ABIERTO";

    public PeriodoRevisionAbiertoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Las evaluaciones de coevaluadores solo pueden consultarse tras el cierre formal del periodo de revisión."
                : mensaje,
            409)
    {
    }
}
