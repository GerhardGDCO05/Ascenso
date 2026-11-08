namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Resultado de una reserva de cupo y presupuesto.
/// </summary>
public sealed record ResultadoReservaPresupuesto(
    Guid ReservaId,
    string Estado,
    decimal Monto,
    int Cupos,
    DateTimeOffset ExpiraEn);
