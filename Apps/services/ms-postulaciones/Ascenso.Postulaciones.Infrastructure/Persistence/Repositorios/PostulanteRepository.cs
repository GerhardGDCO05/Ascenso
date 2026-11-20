using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de postulantes.
/// </summary>
public sealed class PostulanteRepository : IPostulanteRepository
{
    private readonly PostulacionesDbContext _contexto;

    public PostulanteRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<Postulante?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del postulante es obligatorio.", nameof(id));
        }

        return ConsultarPorId(id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<Postulante> ConsultarPorId(Guid id)
        => _contexto.Postulantes
            .AsNoTracking()
            .Include(postulante => postulante.MiembrosFamiliares)
            .Where(postulante => postulante.Id == id);

    public Task<Postulante?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(usuarioId));
        }

        return ConsultarPorUsuarioId(usuarioId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<Postulante> ConsultarPorUsuarioId(Guid usuarioId)
        => _contexto.Postulantes
            .AsNoTracking()
            .Include(postulante => postulante.MiembrosFamiliares)
            .Where(postulante => postulante.UsuarioId == usuarioId);

    public Task<Postulante?> ObtenerParaActualizarPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(usuarioId));
        }

        return ConsultarParaActualizarPorUsuarioId(usuarioId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<Postulante> ConsultarParaActualizarPorUsuarioId(Guid usuarioId)
        => _contexto.Postulantes
            .Include(postulante => postulante.MiembrosFamiliares)
            .Where(postulante => postulante.UsuarioId == usuarioId);

    public Task<bool> ExistePorNumeroDocumentoAsync(
        string numeroDocumentoIdentidad,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numeroDocumentoIdentidad))
        {
            throw new ArgumentException(
                "El número de documento de identidad es obligatorio.",
                nameof(numeroDocumentoIdentidad));
        }

        var documentoNormalizado = numeroDocumentoIdentidad.Trim();

        return _contexto.Postulantes
            .AsNoTracking()
            .AnyAsync(
                postulante => postulante.NumeroDocumentoIdentidad == documentoNormalizado,
                cancellationToken);
    }

    public async Task AgregarAsync(Postulante postulante, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(postulante);
        await _contexto.Postulantes.AddAsync(postulante, cancellationToken);
    }
}
