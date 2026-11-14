namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ConvenioNoEncontradoException : DomainException
{
    public const string CodigoError = "CONVENIO_NO_ENCONTRADO";

    public ConvenioNoEncontradoException(Guid convenioId)
        : base(
            CodigoError,
            404,
            $"No se encontró el convenio con identificador '{convenioId}'.")
    {
        ConvenioId = convenioId;
    }

    public Guid ConvenioId { get; }
}
