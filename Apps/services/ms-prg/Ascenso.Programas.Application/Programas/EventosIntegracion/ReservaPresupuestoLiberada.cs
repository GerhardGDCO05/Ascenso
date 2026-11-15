namespace Ascenso.Programas.Application.Programas.EventosIntegracion;

public sealed record ReservaPresupuestoLiberada(
    Guid EventId,
    Guid ProgramaBecaId,
    Guid ReservaId,
    Guid ReferenciaExternaId,
    string PeriodoAcademicoId,
    decimal MontoLiberado,
    string Motivo,
    decimal PresupuestoDisponibleResultante,
    int CuposDisponiblesResultantes,
    Guid CorrelationId,
    DateTimeOffset OccurredAt);
