using Ascenso.Becas.Domain.Renovacion.Agregados;

namespace Ascenso.Becas.Domain.Ports;

public interface ISolicitudRenovacionRepository
{
    Task<SolicitudRenovacion?> ObtenerPorIdAsync(
        Guid solicitudId, CancellationToken cancellationToken = default);

    Task<SolicitudRenovacion?> ObtenerParaActualizarAsync(
        Guid solicitudId, CancellationToken cancellationToken = default);

    Task<bool> ExistePorBecaYPeriodoAsync(
        Guid becaId, Guid periodoSolicitadoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SolicitudRenovacionVencida>> ObtenerEnCursoVencidasAsync(
        DateOnly fechaActual, int limite, DateOnly? ultimaFecha = null,
        Guid? ultimoId = null,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(SolicitudRenovacion solicitud,
        CancellationToken cancellationToken = default);
}

public sealed record SolicitudRenovacionVencida(Guid Id, DateOnly FechaLimite);
