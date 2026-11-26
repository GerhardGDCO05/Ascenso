namespace Ascenso.Programas.Domain.Excepciones;

public sealed class DocumentoRequeridoNoEncontradoException : DomainException
{
    public const string CodigoError = "DOCUMENTO_REQUERIDO_NO_ENCONTRADO";

    public DocumentoRequeridoNoEncontradoException(Guid convocatoriaId, Guid documentoId)
        : base(
            CodigoError,
            404,
            $"No se encontró el documento requerido '{documentoId}' en la convocatoria '{convocatoriaId}'.")
    {
        ConvocatoriaId = convocatoriaId;
        DocumentoId = documentoId;
    }

    public Guid ConvocatoriaId { get; }

    public Guid DocumentoId { get; }
}
