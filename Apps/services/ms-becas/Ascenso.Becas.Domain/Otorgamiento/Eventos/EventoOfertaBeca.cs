namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

/// <summary>
/// Base de los eventos de dominio de una oferta de beca.
/// </summary>
public abstract record class EventoOfertaBeca : EventoBeca
{
    protected EventoOfertaBeca(
        Guid becaId,
        Guid ofertaId,
        Guid postulacionId,
        Guid programaBecaId,
        Guid periodoAcademicoId,
        Guid correlacionId,
        DateTimeOffset occurredAt)
        : base(becaId, correlacionId, occurredAt)
    {
        if (ofertaId == Guid.Empty)
        {
            throw new ArgumentException("La oferta del evento es obligatoria.", nameof(ofertaId));
        }

        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException("La postulación del evento es obligatoria.", nameof(postulacionId));
        }

        if (programaBecaId == Guid.Empty)
        {
            throw new ArgumentException("El programa del evento es obligatorio.", nameof(programaBecaId));
        }

        if (periodoAcademicoId == Guid.Empty)
        {
            throw new ArgumentException("El periodo del evento es obligatorio.", nameof(periodoAcademicoId));
        }

        OfertaId = ofertaId;
        PostulacionId = postulacionId;
        ProgramaBecaId = programaBecaId;
        PeriodoAcademicoId = periodoAcademicoId;
    }

    public Guid OfertaId { get; }

    public Guid PostulacionId { get; }

    public Guid ProgramaBecaId { get; }

    public Guid PeriodoAcademicoId { get; }
}
