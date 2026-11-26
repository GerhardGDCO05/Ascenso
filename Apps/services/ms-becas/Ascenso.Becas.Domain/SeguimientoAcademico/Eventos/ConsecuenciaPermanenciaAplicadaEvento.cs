using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.SeguimientoAcademico.Eventos;

public sealed record class ConsecuenciaPermanenciaAplicadaEvento : DomainEvent
{
    public ConsecuenciaPermanenciaAplicadaEvento(
        Guid evaluacionPermanenciaId, Guid reporteAcademicoId, Guid becaId,
        ConsecuenciaPermanencia consecuenciaAplicada,
        Guid correlacionId, DateTimeOffset occurredAt)
        : base(correlacionId, occurredAt)
    {
        EvaluacionPermanenciaId = evaluacionPermanenciaId;
        ReporteAcademicoId = reporteAcademicoId;
        BecaId = becaId;
        ConsecuenciaAplicada = consecuenciaAplicada;
    }

    public Guid EvaluacionPermanenciaId { get; }
    public Guid ReporteAcademicoId { get; }
    public Guid BecaId { get; }
    public ConsecuenciaPermanencia ConsecuenciaAplicada { get; }
}
