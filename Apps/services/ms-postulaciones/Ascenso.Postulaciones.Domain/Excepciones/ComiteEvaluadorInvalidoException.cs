namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando los datos de un comité evaluador o de una membresía no son válidos.
/// </summary>
public sealed class ComiteEvaluadorInvalidoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "COMITE_INVALIDO";

    public ComiteEvaluadorInvalidoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Los datos del comité evaluador no son válidos."
                : mensaje,
            422)
    {
    }
}
