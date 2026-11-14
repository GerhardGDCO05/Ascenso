namespace Ascenso.Programas.Domain.Excepciones;

public sealed class DocumentoConvenioPdfRequeridoException : DomainException
{
    public const string CodigoError = "DOCUMENTO_CONVENIO_PDF_REQUERIDO";

    public DocumentoConvenioPdfRequeridoException()
        : base(
            CodigoError,
            400,
            "El documento del convenio en Firebase Storage es obligatorio y debe tener extensión '.pdf'.")
    {
    }
}
