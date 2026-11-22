namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta desvincular la membresía correspondiente a la jefatura del comité.
/// </summary>
public sealed class JefaturaNoDesvinculableException : ExcepcionReglaNegocio
{
    public const string CodigoError = "JEFATURA_NO_DESVINCULABLE";

    public JefaturaNoDesvinculableException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se puede desvincular a la jefatura del comité evaluador."
                : mensaje,
            422)
    {
    }
}
