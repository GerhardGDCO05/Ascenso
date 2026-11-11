namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

public sealed record class OfertaBecaEmitidaEvento : EventoOfertaBeca
{
    public OfertaBecaEmitidaEvento(
        Guid becaId,
        Guid ofertaId,
        Guid postulacionId,
        Guid programaBecaId,
        Guid periodoAcademicoId,
        DateTimeOffset fechaVencimientoAceptacion,
        Guid correlacionId,
        DateTimeOffset occurredAt)
        : base(
            becaId,
            ofertaId,
            postulacionId,
            programaBecaId,
            periodoAcademicoId,
            correlacionId,
            occurredAt)
    {
        if (fechaVencimientoAceptacion == default)
        {
            throw new ArgumentException(
                "La fecha de vencimiento del evento es obligatoria.",
                nameof(fechaVencimientoAceptacion));
        }

        FechaVencimientoAceptacion = fechaVencimientoAceptacion.ToUniversalTime();
    }

    public DateTimeOffset FechaVencimientoAceptacion { get; }
}
