using System.Data;
using Ascenso.Becas.Application.Idempotencia;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del almacén de idempotencia. Usa
/// <c>INSERT ... ON CONFLICT DO NOTHING</c> para resolver carreras entre
/// solicitudes concurrentes sin abortar la transacción.
/// </summary>
public sealed class AlmacenIdempotencia : IAlmacenIdempotencia
{
    private readonly BecasDbContext _contexto;

    public AlmacenIdempotencia(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<RegistroIdempotencia?> ObtenerAsync(
        string endpoint,
        string clave,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);

        var registro = await _contexto.SolicitudesIdempotencia
            .AsNoTracking()
            .SingleOrDefaultAsync(
                solicitud => solicitud.Endpoint == endpoint && solicitud.Clave == clave,
                cancellationToken);

        return registro is null ? null : Mapear(registro);
    }

    public async Task IniciarTransaccionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_contexto.Database.CurrentTransaction is null)
        {
            await _contexto.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
        }
    }

    public async Task<bool> IntentarRegistrarAsync(
        RegistroIdempotencia registro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registro);

        var filas = await _contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO becas.idempotency_request
                (id, endpoint, clave, hash_payload, estado, creado_en, expira_en)
            VALUES
                ({registro.Id}, {registro.Endpoint}, {registro.Clave}, {registro.HashPayload},
                 {registro.Estado}, {registro.CreadoEn}, {registro.ExpiraEn})
            ON CONFLICT (endpoint, clave) DO NOTHING
            """, cancellationToken);

        return filas > 0;
    }

    public Task CompletarAsync(
        string endpoint,
        string clave,
        int statusCode,
        string? responseBody,
        Guid? recursoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);

        return _contexto.SolicitudesIdempotencia
            .Where(solicitud =>
                solicitud.Endpoint == endpoint && solicitud.Clave == clave)
            .ExecuteUpdateAsync(
                actualizacion => actualizacion
                    .SetProperty(solicitud => solicitud.Estado, "COMPLETADA")
                    .SetProperty(solicitud => solicitud.StatusCode, statusCode)
                    .SetProperty(solicitud => solicitud.ResponseBody, responseBody)
                    .SetProperty(solicitud => solicitud.RecursoId, recursoId),
                cancellationToken);
    }

    public Task EliminarAsync(
        string endpoint,
        string clave,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);

        return _contexto.SolicitudesIdempotencia
            .Where(solicitud =>
                solicitud.Endpoint == endpoint && solicitud.Clave == clave)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task ConfirmarAsync(CancellationToken cancellationToken = default)
    {
        if (_contexto.Database.CurrentTransaction is not { } transaccion)
        {
            return;
        }

        await transaccion.CommitAsync(cancellationToken);
        await transaccion.DisposeAsync();
    }

    public async Task RevertirAsync(CancellationToken cancellationToken = default)
    {
        if (_contexto.Database.CurrentTransaction is not { } transaccion)
        {
            return;
        }

        await transaccion.RollbackAsync(cancellationToken);
        await transaccion.DisposeAsync();
    }

    private static RegistroIdempotencia Mapear(IdempotencyRequest registro)
        => new(
            registro.Id,
            registro.Endpoint,
            registro.Clave,
            registro.HashPayload,
            registro.Estado,
            registro.StatusCode,
            registro.ResponseBody,
            registro.RecursoId,
            registro.CreadoEn,
            registro.ExpiraEn);
}
