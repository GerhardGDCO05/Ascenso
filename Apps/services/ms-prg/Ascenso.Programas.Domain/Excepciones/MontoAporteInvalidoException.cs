namespace Ascenso.Programas.Domain.Excepciones;

public sealed class MontoAporteInvalidoException : DomainException
{
    public const string CodigoError = "MONTO_APORTE_INVALIDO";

    public MontoAporteInvalidoException(decimal montoAporte)
        : base(
            CodigoError,
            400,
            $"El monto del aporte ({montoAporte:0.00}) debe ser mayor a cero y admitir como máximo dos decimales.")
    {
        MontoAporte = montoAporte;
    }

    public decimal MontoAporte { get; }
}

