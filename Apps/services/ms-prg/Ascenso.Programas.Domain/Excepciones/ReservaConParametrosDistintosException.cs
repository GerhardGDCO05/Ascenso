namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ReservaConParametrosDistintosException : DomainException
{
    public const string CodigoError = "RESERVA_CON_PARAMETROS_DISTINTOS";

    public ReservaConParametrosDistintosException(Guid programaId, Guid referenciaExternaId)
        : base(
            CodigoError,
            409,
            $"Ya existe una reserva en el programa '{programaId}' para la referencia externa '{referenciaExternaId}' con un monto o estado incompatible.")
    {
        ProgramaId = programaId;
        ReferenciaExternaId = referenciaExternaId;
    }

    public Guid ProgramaId { get; }

    public Guid ReferenciaExternaId { get; }
}
