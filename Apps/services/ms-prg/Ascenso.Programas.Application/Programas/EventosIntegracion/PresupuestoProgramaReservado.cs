namespace Ascenso.Programas.Application.Programas.EventosIntegracion;

public sealed record PresupuestoProgramaReservado(
    Guid EventId,
    Guid ProgramaBecaId,
    Guid ReservaId,
    Guid ReferenciaExternaId,
    string PeriodoAcademicoId,
    decimal MontoReservado,
    decimal PresupuestoDisponibleResultante,
    int CuposDisponiblesResultantes,
    Guid CorrelationId,
    DateTimeOffset OccurredAt);
