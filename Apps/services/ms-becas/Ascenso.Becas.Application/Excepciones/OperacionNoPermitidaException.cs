namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Se produce cuando el actor autenticado no puede ejecutar la operación.
/// </summary>
public sealed class OperacionNoPermitidaException : AplicacionException
{
    public const string CodigoError = "PERMISO_DENEGADO";

    public OperacionNoPermitidaException(string mensaje)
        : base(CodigoError, 403, mensaje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mensaje);
    }
}
