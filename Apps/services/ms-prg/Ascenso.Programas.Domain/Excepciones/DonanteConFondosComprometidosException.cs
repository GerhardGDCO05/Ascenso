namespace Ascenso.Programas.Domain.Excepciones;

public sealed class DonanteConFondosComprometidosException : DomainException
{
    public const string CodigoError = "DONANTE_CON_FONDOS";

    public DonanteConFondosComprometidosException(Guid donanteId)
        : base(
            CodigoError,
            409,
            $"El donante '{donanteId}' no puede desactivarse porque mantiene fondos comprometidos en programas vigentes (RN-PRG-04).")
    {
        DonanteId = donanteId;
    }

    public Guid DonanteId { get; }
}

