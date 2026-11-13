using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositorio de las solicitudes idempotentes. No existe un puerto de dominio
/// porque la idempotencia es una preocupación transversal de infraestructura.
/// </summary>
public sealed class IdempotencyRepository
{
    private readonly BecasDbContext _contexto;

    public IdempotencyRepository(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<IdempotencyRequest?> ObtenerAsync(
        string endpoint,
        string clave,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);

        return _contexto.SolicitudesIdempotencia
            .SingleOrDefaultAsync(
                solicitud => solicitud.Endpoint == endpoint && solicitud.Clave == clave,
                cancellationToken);
    }

    public Task<bool> ExisteAsync(
        string endpoint,
        string clave,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);

        return _contexto.SolicitudesIdempotencia
            .AsNoTracking()
            .AnyAsync(
                solicitud => solicitud.Endpoint == endpoint && solicitud.Clave == clave,
                cancellationToken);
    }

    public async Task AgregarAsync(
        IdempotencyRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        await _contexto.SolicitudesIdempotencia.AddAsync(solicitud, cancellationToken);
    }

    public Task<int> EliminarExpiradasAsync(
        DateTimeOffset ahora,
        CancellationToken cancellationToken = default)
        => _contexto.SolicitudesIdempotencia
            .Where(solicitud => solicitud.ExpiraEn <= ahora)
            .ExecuteDeleteAsync(cancellationToken);
}
