namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ConvocatoriaNoEncontradaException : DomainException
{
    public const string CodigoError = "CONVOCATORIA_NO_ENCONTRADA";

    public ConvocatoriaNoEncontradaException(Guid convocatoriaId)
        : base(
            CodigoError,
            404,
            $"No se encontró la convocatoria con identificador '{convocatoriaId}'.")
    {
        ConvocatoriaId = convocatoriaId;
    }

    public Guid ConvocatoriaId { get; }
}
