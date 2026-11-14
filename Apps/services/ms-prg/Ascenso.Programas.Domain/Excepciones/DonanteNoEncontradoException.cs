namespace Ascenso.Programas.Domain.Excepciones;

public sealed class DonanteNoEncontradoException : DomainException
{
    public const string CodigoError = "DONANTE_NO_ENCONTRADO";

    public DonanteNoEncontradoException(Guid donanteId)
        : base(
            CodigoError,
            404,
            $"No se encontró el donante con identificador '{donanteId}'.")
    {
        DonanteId = donanteId;
    }

    public Guid DonanteId { get; }
}

