namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando un tipo de caso límite o sus invariantes de dominio resultan inconsistentes.
/// </summary>
public sealed class CasoLimiteInvalidoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CASO_LIMITE_INVALIDO";

    public CasoLimiteInvalidoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Los datos del caso límite de ranking indicado no son válidos."
                : mensaje,
            422)
    {
    }
}
