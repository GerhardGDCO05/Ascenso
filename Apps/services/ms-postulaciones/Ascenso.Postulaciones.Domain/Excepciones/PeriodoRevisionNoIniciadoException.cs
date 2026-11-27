namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta guardar o enviar una evaluación antes del inicio del periodo de revisión.
/// </summary>
public sealed class PeriodoRevisionNoIniciadoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "PERIODO_REVISION_NO_INICIADO";

    public PeriodoRevisionNoIniciadoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El periodo formal de revisión de la convocatoria aún no ha iniciado."
                : mensaje,
            409)
    {
    }
}
