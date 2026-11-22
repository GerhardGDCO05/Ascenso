using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de comités evaluadores.
/// </summary>
public sealed class ComiteEvaluadorRepository : IComiteEvaluadorRepository
{
    private readonly PostulacionesDbContext _contexto;

    public ComiteEvaluadorRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<ComiteEvaluador?> ObtenerPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del comité evaluador es obligatorio.", nameof(id));
        }

        return ConsultarPorId(id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<ComiteEvaluador> ConsultarPorId(Guid id)
        => _contexto.ComitesEvaluadores
            .AsNoTracking()
            .Include(comite => comite.Miembros
                .OrderBy(miembro => miembro.FechaAsignacion)
                .ThenBy(miembro => miembro.Id))
            .Where(comite => comite.Id == id);

    public Task<ComiteEvaluador?> ObtenerParaActualizarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del comité evaluador es obligatorio.", nameof(id));
        }

        return ConsultarParaActualizarPorId(id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<ComiteEvaluador> ConsultarParaActualizarPorId(Guid id)
        => _contexto.ComitesEvaluadores
            .Include(comite => comite.Miembros
                .OrderBy(miembro => miembro.FechaAsignacion)
                .ThenBy(miembro => miembro.Id))
            .Where(comite => comite.Id == id);

    public Task<ComiteEvaluador?> ObtenerPorProgramaYConvocatoriaAsync(
        Guid programaBecaId,
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        ValidarProgramaYConvocatoria(programaBecaId, convocatoriaId);

        return ConsultarPorProgramaYConvocatoria(programaBecaId, convocatoriaId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<ComiteEvaluador> ConsultarPorProgramaYConvocatoria(
        Guid programaBecaId,
        Guid convocatoriaId)
        => _contexto.ComitesEvaluadores
            .AsNoTracking()
            .Include(comite => comite.Miembros
                .OrderBy(miembro => miembro.FechaAsignacion)
                .ThenBy(miembro => miembro.Id))
            .Where(comite => comite.ProgramaBecaId == programaBecaId
                && comite.ConvocatoriaId == convocatoriaId);

    public Task<bool> ExistePorProgramaYConvocatoriaAsync(
        Guid programaBecaId,
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        ValidarProgramaYConvocatoria(programaBecaId, convocatoriaId);

        return _contexto.ComitesEvaluadores
            .AsNoTracking()
            .AnyAsync(
                comite => comite.ProgramaBecaId == programaBecaId
                    && comite.ConvocatoriaId == convocatoriaId,
                cancellationToken);
    }

    public async Task AgregarAsync(
        ComiteEvaluador comite,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comite);
        await _contexto.ComitesEvaluadores.AddAsync(comite, cancellationToken);
    }

    private static void ValidarProgramaYConvocatoria(Guid programaBecaId, Guid convocatoriaId)
    {
        if (programaBecaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del programa de beca es obligatorio.",
                nameof(programaBecaId));
        }

        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }
    }
}
