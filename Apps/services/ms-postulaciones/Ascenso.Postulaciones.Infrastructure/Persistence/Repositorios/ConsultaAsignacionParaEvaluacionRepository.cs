using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del puerto segregado de consulta de asignaciones para el flujo de evaluación.
/// </summary>
public sealed class ConsultaAsignacionParaEvaluacionRepository : IConsultaAsignacionParaEvaluacionRepository
{
    private readonly PostulacionesDbContext _contexto;

    public ConsultaAsignacionParaEvaluacionRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<AsignacionEvaluacion?> ObtenerPorIdAsync(
        Guid asignacionEvaluacionId,
        CancellationToken cancellationToken = default)
    {
        if (asignacionEvaluacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la asignación de evaluación es obligatorio.",
                nameof(asignacionEvaluacionId));
        }

        return ConsultarPorId(asignacionEvaluacionId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<AsignacionEvaluacion> ConsultarPorId(Guid asignacionEvaluacionId)
        => _contexto.AsignacionesEvaluacion
            .AsNoTracking()
            .Where(asignacion => asignacion.Id == asignacionEvaluacionId);

    public Task<AsignacionEvaluacion?> ObtenerParaActualizarPorIdAsync(
        Guid asignacionEvaluacionId,
        CancellationToken cancellationToken = default)
    {
        if (asignacionEvaluacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la asignación de evaluación es obligatorio.",
                nameof(asignacionEvaluacionId));
        }

        return ConsultarParaActualizarPorId(asignacionEvaluacionId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<AsignacionEvaluacion> ConsultarParaActualizarPorId(Guid asignacionEvaluacionId)
        => _contexto.AsignacionesEvaluacion
            .Where(asignacion => asignacion.Id == asignacionEvaluacionId);
}
