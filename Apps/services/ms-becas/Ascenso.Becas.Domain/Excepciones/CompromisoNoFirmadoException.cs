namespace Ascenso.Becas.Domain.Excepciones;

public sealed class CompromisoNoFirmadoException : DomainException
{
    public const string CodigoError = "COMPROMISO_NO_FIRMADO";

    public CompromisoNoFirmadoException(Guid becaId)
        : base(
            CodigoError,
            412,
            $"La beca '{becaId}' no tiene un compromiso firmado aprobado y no puede habilitar desembolsos.")
    {
        BecaId = becaId;
    }

    public Guid BecaId { get; }
}
