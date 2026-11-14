namespace Ascenso.Programas.Application.Programas.EventosIntegracion;

public sealed record PresupuestoProgramaConfirmado(
    Guid EventId,
    Guid ProgramaBecaId,
    Guid ReservaId,
    Guid ReferenciaExternaId,
    string PeriodoAcademicoId,
    decimal MontoConfirmado,
    decimal PresupuestoEjecutadoResultante,
    int CuposOcupadosResultantes,
    Guid CorrelationId,
    DateTimeOffset OccurredAt);
