namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ReservaPresupuestariaNoEncontradaException : DomainException
{
    public const string CodigoError = "RESERVA_PRESUPUESTARIA_NO_ENCONTRADA";

    public ReservaPresupuestariaNoEncontradaException(Guid programaId, Guid referenciaExternaId)
        : base(
            CodigoError,
            404,
            $"No se encontró una reserva presupuestaria en el programa '{programaId}' asociada a la referencia externa '{referenciaExternaId}'.")
    {
        ProgramaId = programaId;
        ReferenciaExternaId = referenciaExternaId;
    }

    public Guid ProgramaId { get; }

    public Guid ReferenciaExternaId { get; }
}
