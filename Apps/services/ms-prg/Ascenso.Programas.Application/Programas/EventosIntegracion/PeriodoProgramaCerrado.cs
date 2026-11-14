namespace Ascenso.Programas.Application.Programas.EventosIntegracion;

public sealed record PeriodoProgramaCerrado(
    Guid EventId,
    Guid ProgramaBecaId,
    string PeriodoAcademicoId,
    decimal SaldoNoEjecutadoConsolidado,
    decimal PresupuestoEjecutadoTotal,
    string EstadoResultante,
    Guid CorrelationId,
    DateTimeOffset OccurredAt);
