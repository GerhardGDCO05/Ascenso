using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.SeguimientoAcademico.Eventos;

/// <summary>Señal a MS-PLT para avisar a la coordinación del programa.</summary>
public sealed record class AlertaReporteAcademicoTardioEvento : DomainEvent
{
    public AlertaReporteAcademicoTardioEvento(
        Guid reporteAcademicoId, Guid becaId, Guid programaBecaId,
        Guid periodoAcademicoId, DateOnly fechaLimitePeriodo,
        Guid correlacionId, DateTimeOffset occurredAt)
        : base(correlacionId, occurredAt)
    {
        ReporteAcademicoId = reporteAcademicoId;
        BecaId = becaId;
        ProgramaBecaId = programaBecaId;
        PeriodoAcademicoId = periodoAcademicoId;
        FechaLimitePeriodo = fechaLimitePeriodo;
    }

    public Guid ReporteAcademicoId { get; }
    public Guid BecaId { get; }
    public Guid ProgramaBecaId { get; }
    public Guid PeriodoAcademicoId { get; }
    public DateOnly FechaLimitePeriodo { get; }
}
