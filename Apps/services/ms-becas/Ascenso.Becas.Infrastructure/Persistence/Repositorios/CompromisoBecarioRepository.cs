using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Ascenso.Becas.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del puerto de la entidad <c>CompromisoBecario</c>.
/// </summary>
public sealed class CompromisoBecarioRepository : ICompromisoBecarioRepository
{
    private readonly BecasDbContext _contexto;

    public CompromisoBecarioRepository(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<CompromisoBecario?> ObtenerPorIdAsync(
        Guid compromisoId,
        CancellationToken cancellationToken = default)
    {
        if (compromisoId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del compromiso es obligatorio.",
                nameof(compromisoId));
        }

        return _contexto.CompromisosBecarios
            .SingleOrDefaultAsync(compromiso => compromiso.Id == compromisoId, cancellationToken);
    }

    public Task<CompromisoBecario?> ObtenerPorBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
    {
        if (becaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la beca es obligatorio.", nameof(becaId));
        }

        return _contexto.CompromisosBecarios
            .SingleOrDefaultAsync(compromiso => compromiso.BecaId == becaId, cancellationToken);
    }

    public async Task AgregarAsync(
        CompromisoBecario compromiso,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compromiso);
        await _contexto.CompromisosBecarios.AddAsync(compromiso, cancellationToken);
    }

    public Task<bool> ExisteAsync(Guid compromisoId, CancellationToken cancellationToken = default)
        => _contexto.CompromisosBecarios
            .AsNoTracking()
            .AnyAsync(compromiso => compromiso.Id == compromisoId, cancellationToken);

    public Task<bool> ExisteAprobadoParaBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
        => _contexto.CompromisosBecarios
            .AsNoTracking()
            .AnyAsync(
                compromiso => compromiso.BecaId == becaId
                    && compromiso.Estado == EstadoCompromiso.Aprobado,
                cancellationToken);
}
