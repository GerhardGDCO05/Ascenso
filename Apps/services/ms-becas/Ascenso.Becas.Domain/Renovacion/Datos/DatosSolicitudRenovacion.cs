namespace Ascenso.Becas.Domain.Renovacion.Datos;

public sealed record class DatosSolicitudRenovacion(
    Guid BecaId,
    Guid PeriodoOrigenId,
    Guid PeriodoSolicitadoId,
    DateOnly FechaLimiteVentanaRenovacion,
    bool CumpleRendimientoAcademico,
    bool TienePresupuestoDisponible,
    decimal MontoSolicitado);
