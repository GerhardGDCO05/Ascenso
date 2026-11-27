namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de reserva de cupo y presupuesto contra el microservicio de programas.
/// </summary>
public interface IProgramasClient
{
    Task<ResultadoCondicionesPrograma?> ObtenerCondicionesAsync(
        Guid programaBecaId,
        Guid carreraId,
        Guid periodoAcademicoId,
        Guid universidadAliadaId,
        CancellationToken cancellationToken = default);

    Task<ResultadoReservaPresupuesto> ReservarAsync(
        Guid programaBecaId,
        SolicitudReservaPresupuesto solicitud,
        CancellationToken cancellationToken = default);

    Task ConfirmarReservaAsync(
        Guid programaBecaId,
        Guid reservaId,
        Guid ofertaId,
        CancellationToken cancellationToken = default);

    Task ActivarReservaAsync(
        Guid programaBecaId,
        Guid reservaId,
        Guid ofertaId,
        CancellationToken cancellationToken = default);

    Task LiberarReservaAsync(
        Guid programaBecaId,
        Guid reservaId,
        Guid ofertaId,
        CancellationToken cancellationToken = default);
}
