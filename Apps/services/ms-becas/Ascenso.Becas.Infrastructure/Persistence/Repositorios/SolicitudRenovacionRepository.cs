using Ascenso.Becas.Domain.Enums;
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

    public async Task<IReadOnlyList<SolicitudRenovacionVencida>> ObtenerEnCursoVencidasAsync(
        DateOnly fechaActual, int limite, DateOnly? ultimaFecha = null,
        Guid? ultimoId = null,
        CancellationToken cancellationToken = default)
    {
        if (fechaActual == default || limite <= 0)
            throw new ArgumentException("La fecha y el límite deben ser válidos.");
        var consulta = _contexto.SolicitudesRenovacion.AsNoTracking()
            .Where(solicitud => solicitud.Estado == EstadoRenovacion.SolicitadaEnCurso
                && solicitud.FechaLimiteVentanaRenovacion < fechaActual);
        if (ultimaFecha is not null && ultimoId is not null)
            consulta = consulta.Where(solicitud =>
                solicitud.FechaLimiteVentanaRenovacion > ultimaFecha.Value
                || (solicitud.FechaLimiteVentanaRenovacion == ultimaFecha.Value
                    && solicitud.Id.CompareTo(ultimoId.Value) > 0));
        return await consulta
            .OrderBy(solicitud => solicitud.FechaLimiteVentanaRenovacion)
            .ThenBy(solicitud => solicitud.Id)
            .Take(limite)
            .Select(solicitud => new SolicitudRenovacionVencida(
                solicitud.Id, solicitud.FechaLimiteVentanaRenovacion))
            .ToArrayAsync(cancellationToken);
    }

    public async Task AgregarAsync(SolicitudRenovacion solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        await _contexto.SolicitudesRenovacion.AddAsync(solicitud, cancellationToken);
    }
}
