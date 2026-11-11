namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

public sealed record class OfertaBecaExpiradaEvento : EventoOfertaBeca
{
    public OfertaBecaExpiradaEvento(
        Guid becaId,
        Guid ofertaId,
        Guid postulacionId,
        Guid programaBecaId,
        Guid periodoAcademicoId,
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
    }
}
