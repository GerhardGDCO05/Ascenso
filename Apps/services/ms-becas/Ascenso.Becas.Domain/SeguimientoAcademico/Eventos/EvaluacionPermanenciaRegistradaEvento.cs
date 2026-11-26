using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.SeguimientoAcademico.Eventos;

public sealed record class EvaluacionPermanenciaRegistradaEvento : DomainEvent
{
    public EvaluacionPermanenciaRegistradaEvento(
        Guid evaluacionPermanenciaId, Guid reporteAcademicoId, Guid becaId,
        bool cumpleCondiciones, ConsecuenciaPermanencia? consecuenciaRecomendada,
        Guid correlacionId, DateTimeOffset occurredAt)
        : base(correlacionId, occurredAt)
    {
        EvaluacionPermanenciaId = evaluacionPermanenciaId;
        ReporteAcademicoId = reporteAcademicoId;
        BecaId = becaId;
        CumpleCondiciones = cumpleCondiciones;
        ConsecuenciaRecomendada = consecuenciaRecomendada;
    }

    public Guid EvaluacionPermanenciaId { get; }
    public Guid ReporteAcademicoId { get; }
    public Guid BecaId { get; }
    public bool CumpleCondiciones { get; }
    public ConsecuenciaPermanencia? ConsecuenciaRecomendada { get; }
}
