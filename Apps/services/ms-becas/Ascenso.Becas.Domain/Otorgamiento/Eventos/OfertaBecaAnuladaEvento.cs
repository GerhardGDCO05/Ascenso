namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

public sealed record class OfertaBecaAnuladaEvento : EventoOfertaBeca
{
    public OfertaBecaAnuladaEvento(
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
