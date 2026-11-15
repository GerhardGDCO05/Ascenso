using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Excepciones;

public sealed class TransicionEstadoReservaInvalidaException : DomainException
{
    public const string CodigoError = "TRANSICION_ESTADO_RESERVA_INVALIDA";

    public TransicionEstadoReservaInvalidaException(
        Guid referenciaExternaId,
        EstadoReservaPresupuestaria estadoActual,
        EstadoReservaPresupuestaria estadoDestino)
        : base(
            CodigoError,
            409,
            $"No es posible cambiar la reserva '{referenciaExternaId}' del estado '{estadoActual}' a '{estadoDestino}'.")
    {
        ReferenciaExternaId = referenciaExternaId;
        EstadoActual = estadoActual;
        EstadoDestino = estadoDestino;
    }

    public Guid ReferenciaExternaId { get; }

    public EstadoReservaPresupuestaria EstadoActual { get; }

    public EstadoReservaPresupuestaria EstadoDestino { get; }
}
