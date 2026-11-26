using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del puerto de solo lectura para consultar asignaciones vigentes,
/// evaluaciones definitivas y excepción de evaluador único de una postulación.
/// </summary>
public sealed class ConsultaDatosConsolidacionRepository : IConsultaDatosConsolidacionRepository
{
    private readonly PostulacionesDbContext _contexto;

    public ConsultaDatosConsolidacionRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<IReadOnlyList<AsignacionEvaluacion>> ListarAsignacionesVigentesPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        ValidarPostulacionId(postulacionId);

        return await ConsultarAsignacionesVigentesPorPostulacion(postulacionId)
            .ToListAsync(cancellationToken);
    }

    internal IQueryable<AsignacionEvaluacion> ConsultarAsignacionesVigentesPorPostulacion(Guid postulacionId)
        => _contexto.AsignacionesEvaluacion
            .AsNoTracking()
            .Where(asignacion => asignacion.PostulacionId == postulacionId
                && asignacion.Estado != EstadoAsignacionEvaluacion.ReasignadaPorConflicto);

    public async Task<IReadOnlyList<Evaluacion>> ListarEvaluacionesDefinitivasPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        ValidarPostulacionId(postulacionId);

        return await ConsultarEvaluacionesDefinitivasPorPostulacion(postulacionId)
            .ToListAsync(cancellationToken);
    }

    internal IQueryable<Evaluacion> ConsultarEvaluacionesDefinitivasPorPostulacion(Guid postulacionId)
        => _contexto.Evaluaciones
            .AsNoTracking()
            .Where(evaluacion => evaluacion.PostulacionId == postulacionId
                && evaluacion.Estado == EstadoEvaluacion.EnviadaDefinitiva)
            .OrderBy(evaluacion => evaluacion.FechaEnvio)
            .ThenBy(evaluacion => evaluacion.Id);

    public Task<AutorizacionExcepcionEvaluadorUnico?> ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        ValidarPostulacionId(postulacionId);

        return ConsultarExcepcionEvaluadorUnicoPorPostulacion(postulacionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<AutorizacionExcepcionEvaluadorUnico> ConsultarExcepcionEvaluadorUnicoPorPostulacion(Guid postulacionId)
        => _contexto.ExcepcionesEvaluadorUnico
            .AsNoTracking()
            .Where(autorizacion => autorizacion.PostulacionId == postulacionId);

    private static void ValidarPostulacionId(Guid postulacionId)
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }
    }
}
