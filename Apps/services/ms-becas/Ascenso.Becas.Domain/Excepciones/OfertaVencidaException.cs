namespace Ascenso.Becas.Domain.Excepciones;

public sealed class OfertaVencidaException : DomainException
{
    public const string CodigoError = "OFERTA_VENCIDA";

    public OfertaVencidaException(
        Guid ofertaId,
        DateTimeOffset fechaVencimiento,
        DateTimeOffset fechaActual)
        : base(
            CodigoError,
            409,
            $"La oferta '{ofertaId}' venció el {fechaVencimiento:O} y no puede responderse el {fechaActual:O}.")
    {
        OfertaId = ofertaId;
        FechaVencimiento = fechaVencimiento.ToUniversalTime();
        FechaActual = fechaActual.ToUniversalTime();
    }

    public Guid OfertaId { get; }

    public DateTimeOffset FechaVencimiento { get; }

    public DateTimeOffset FechaActual { get; }
}
