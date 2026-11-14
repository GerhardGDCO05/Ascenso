namespace Ascenso.Programas.Domain.Excepciones;

public sealed class SaldoDonanteInsuficienteException : DomainException
{
    public const string CodigoError = "SALDO_DONANTE_INSUFICIENTE";

    public SaldoDonanteInsuficienteException(Guid donanteId, decimal saldoDisponible, decimal montoSolicitado)
        : base(
            CodigoError,
            409,
            $"El donante '{donanteId}' posee un saldo disponible de {saldoDisponible:0.00} USD, insuficiente para asignar {montoSolicitado:0.00} USD (RN-PRG-02).")
    {
        DonanteId = donanteId;
        SaldoDisponible = saldoDisponible;
        MontoSolicitado = montoSolicitado;
    }

    public Guid DonanteId { get; }

    public decimal SaldoDisponible { get; }

    public decimal MontoSolicitado { get; }
}
