namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Solicitud de reserva de cupo y presupuesto a programas.
/// </summary>
public sealed record SolicitudReservaPresupuesto(
    string ReferenciaTipo,
    Guid ReferenciaId,
    Guid PeriodoAcademicoId,
    decimal Monto,
    int Cupos);
