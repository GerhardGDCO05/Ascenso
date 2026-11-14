namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ConvenioUniversidadVencidoException : DomainException
{
    public const string CodigoError = "CONVENIO_VENCIDO";

    public ConvenioUniversidadVencidoException(Guid universidadId)
        : base(
            CodigoError,
            422,
            $"La universidad aliada '{universidadId}' no posee un convenio vigente para nuevas postulaciones u ofertas (RN-PRG-05).")
    {
        UniversidadId = universidadId;
    }

    public Guid UniversidadId { get; }
}
