namespace Ascenso.Becas.Domain.Excepciones;

public sealed class TransicionEstadoInvalidaException : DomainException
{
    public const string CodigoError = "TRANSICION_INVALIDA";

    public TransicionEstadoInvalidaException(Enum estadoActual, Enum estadoNuevo)
        : this(estadoActual.ToString(), estadoNuevo.ToString())
    {
    }

    public TransicionEstadoInvalidaException(string estadoActual, string estadoNuevo)
        : base(
            CodigoError,
            409,
            $"No se permite la transición de estado de '{estadoActual}' a '{estadoNuevo}'.")
    {
        EstadoActual = estadoActual;
        EstadoNuevo = estadoNuevo;
    }

    public string EstadoActual { get; }

    public string EstadoNuevo { get; }
}
