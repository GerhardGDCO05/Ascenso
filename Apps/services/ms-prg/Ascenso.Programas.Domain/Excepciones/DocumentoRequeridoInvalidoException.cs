namespace Ascenso.Programas.Domain.Excepciones;

public sealed class DocumentoRequeridoInvalidoException : DomainException
{
    public const string CodigoError = "DOCUMENTO_REQUERIDO_INVALIDO";

    public DocumentoRequeridoInvalidoException(string motivo)
        : base(
            CodigoError,
            400,
            $"El documento requerido de la convocatoria no es válido: {motivo}")
    {
        Motivo = motivo;
    }

    public string Motivo { get; }
}
