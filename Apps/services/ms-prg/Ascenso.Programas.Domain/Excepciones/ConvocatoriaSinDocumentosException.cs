namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ConvocatoriaSinDocumentosException : DomainException
{
    public const string CodigoError = "CONVOCATORIA_SIN_DOCUMENTOS";

    public ConvocatoriaSinDocumentosException(Guid convocatoriaId)
        : base(
            CodigoError,
            422,
            $"La convocatoria '{convocatoriaId}' no puede publicarse sin al menos un documento requerido.")
    {
        ConvocatoriaId = convocatoriaId;
    }

    public Guid ConvocatoriaId { get; }
}
