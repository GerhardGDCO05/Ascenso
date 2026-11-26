using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.SeguimientoAcademico.Eventos;

public sealed record class ReporteAcademicoEntregadoEvento : DomainEvent
{
    public ReporteAcademicoEntregadoEvento(
        Guid reporteAcademicoId, Guid becaId, Guid periodoAcademicoId,
        DateOnly fechaLimitePeriodo, bool entregaTardia,
        Guid correlacionId, DateTimeOffset occurredAt)
        : base(correlacionId, occurredAt)
    {
        ReporteAcademicoId = reporteAcademicoId;
        BecaId = becaId;
        PeriodoAcademicoId = periodoAcademicoId;
        FechaLimitePeriodo = fechaLimitePeriodo;
        EntregaTardia = entregaTardia;
    }

    public Guid ReporteAcademicoId { get; }
    public Guid BecaId { get; }
    public Guid PeriodoAcademicoId { get; }
    public DateOnly FechaLimitePeriodo { get; }
    public bool EntregaTardia { get; }
}
