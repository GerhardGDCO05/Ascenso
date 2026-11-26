using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.Renovacion.Agregados;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

public sealed class SolicitudRenovacionRepository : ISolicitudRenovacionRepository
{
    private readonly BecasDbContext _contexto;

    public SolicitudRenovacionRepository(BecasDbContext contexto)
        => _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));

    public Task<SolicitudRenovacion?> ObtenerPorIdAsync(
        Guid solicitudId, CancellationToken cancellationToken = default)
        => _contexto.SolicitudesRenovacion.SingleOrDefaultAsync(
            solicitud => solicitud.Id == solicitudId, cancellationToken);

    public async Task<SolicitudRenovacion?> ObtenerParaActualizarAsync(
        Guid solicitudId, CancellationToken cancellationToken = default)
    {
        if (_contexto.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "La solicitud de renovación requiere una transacción activa para bloquearla.");
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM becas.solicitud_renovacion WHERE id = {solicitudId} FOR UPDATE",
            cancellationToken);
        return await ObtenerPorIdAsync(solicitudId, cancellationToken);
    }

    public Task<bool> ExistePorBecaYPeriodoAsync(
        Guid becaId, Guid periodoSolicitadoId,
        CancellationToken cancellationToken = default)
        => _contexto.SolicitudesRenovacion.AsNoTracking().AnyAsync(
            solicitud => solicitud.BecaId == becaId
                && solicitud.PeriodoSolicitadoId == periodoSolicitadoId,
            cancellationToken);

    public async Task AgregarAsync(SolicitudRenovacion solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        await _contexto.SolicitudesRenovacion.AddAsync(solicitud, cancellationToken);
    }
}
