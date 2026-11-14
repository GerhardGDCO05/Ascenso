namespace Ascenso.Programas.Application.Programas.EventosIntegracion;

public sealed record PresupuestoProgramaAsignado(
    Guid EventoId,
    Guid ProgramaBecaId,
    Guid AsignacionId,
    Guid DonanteId,
    string PeriodoAcademicoId,
    decimal MontoAsignado,
    decimal PresupuestoTotalResultante,
    Guid CorrelacionId,
    DateTimeOffset OcurridoEn);
