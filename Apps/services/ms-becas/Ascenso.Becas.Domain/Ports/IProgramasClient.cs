namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de reserva de cupo y presupuesto contra el microservicio de programas.
/// </summary>
public interface IProgramasClient
{
    Task<ResultadoReservaPresupuesto> ReservarAsync(
        Guid programaBecaId,
        SolicitudReservaPresupuesto solicitud,
        CancellationToken cancellationToken = default);
}
