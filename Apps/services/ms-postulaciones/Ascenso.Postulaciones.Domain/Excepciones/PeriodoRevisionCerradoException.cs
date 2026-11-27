namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta guardar o enviar una evaluación tras el cierre formal del periodo de revisión.
/// </summary>
public sealed class PeriodoRevisionCerradoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "PERIODO_REVISION_CERRADO";

    public PeriodoRevisionCerradoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El periodo formal de revisión de la convocatoria ya se encuentra cerrado."
                : mensaje,
            409)
    {
    }
}
