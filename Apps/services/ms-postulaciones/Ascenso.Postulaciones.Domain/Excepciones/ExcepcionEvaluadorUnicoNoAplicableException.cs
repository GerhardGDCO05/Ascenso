namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta autorizar una excepción de evaluador único para una postulación que ya cuenta con dos o más asignaciones vigentes.
/// </summary>
public sealed class ExcepcionEvaluadorUnicoNoAplicableException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EXCEPCION_NO_APLICABLE";

    public ExcepcionEvaluadorUnicoNoAplicableException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La postulación ya cuenta con al menos dos evaluadores vigentes, por lo que no aplica la excepción de evaluador único."
                : mensaje,
            422)
    {
    }
}
