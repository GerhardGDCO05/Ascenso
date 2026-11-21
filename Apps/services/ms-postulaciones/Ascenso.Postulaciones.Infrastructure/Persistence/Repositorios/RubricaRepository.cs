using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de rúbricas de evaluación.
/// </summary>
public sealed class RubricaRepository : IRubricaRepository
{
    private readonly PostulacionesDbContext _contexto;

    public RubricaRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<RubricaEvaluacion?> ObtenerPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la rúbrica es obligatorio.", nameof(id));
        }

        return ConsultarPorId(id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<RubricaEvaluacion> ConsultarPorId(Guid id)
        => _contexto.Rubricas
            .AsNoTracking()
            .Include(rubrica => rubrica.Criterios)
            .Where(rubrica => rubrica.Id == id);

    public Task<RubricaEvaluacion?> ObtenerParaActualizarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la rúbrica es obligatorio.", nameof(id));
        }

        return ConsultarParaActualizarPorId(id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<RubricaEvaluacion> ConsultarParaActualizarPorId(Guid id)
        => _contexto.Rubricas
            .Include(rubrica => rubrica.Criterios)
            .Where(rubrica => rubrica.Id == id);

    public Task<bool> ExistePorProgramaYConvocatoriaAsync(
        Guid programaBecaId,
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
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

        return _contexto.Rubricas
            .AsNoTracking()
            .AnyAsync(
                rubrica => rubrica.ProgramaBecaId == programaBecaId
                    && rubrica.ConvocatoriaId == convocatoriaId,
                cancellationToken);
    }

    public async Task AgregarAsync(
        RubricaEvaluacion rubrica,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        await _contexto.Rubricas.AddAsync(rubrica, cancellationToken);
    }
}
