namespace Ascenso.Programas.Domain.Excepciones;

public sealed class AsignacionFondoNoEncontradaException : DomainException
{
    public const string CodigoError = "ASIGNACION_FONDO_NO_ENCONTRADA";

    public AsignacionFondoNoEncontradaException(Guid asignacionId)
        : base(
            CodigoError,
            404,
            $"No se encontró la asignación presupuestaria con identificador '{asignacionId}'.")
    {
        AsignacionId = asignacionId;
    }

    public Guid AsignacionId { get; }
}
