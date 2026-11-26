using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del puerto de verificación de evaluaciones pendientes o postulaciones sin consolidar en una convocatoria.
/// </summary>
public sealed class ConsultaEstadoEvaluacionesConvocatoriaRepository : IConsultaEstadoEvaluacionesConvocatoriaRepository
{
    private readonly PostulacionesDbContext _contexto;

    public ConsultaEstadoEvaluacionesConvocatoriaRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<bool> ExistenEvaluacionesPendientesOSinConsolidarAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        ValidarConvocatoriaId(convocatoriaId);

        var existenAsignacionesPendientes = await ConsultarAsignacionesVigentesPendientes(convocatoriaId)
            .AnyAsync(cancellationToken);

        if (existenAsignacionesPendientes)
        {
            return true;
        }

        return await ConsultarAsignacionesVigentesSinResultadoConsolidado(convocatoriaId)
            .AnyAsync(cancellationToken);
    }

    internal IQueryable<AsignacionEvaluacion> ConsultarAsignacionesVigentesPendientes(Guid convocatoriaId)
        => _contexto.AsignacionesEvaluacion
            .AsNoTracking()
            .Where(asignacion => asignacion.ConvocatoriaId == convocatoriaId
                && asignacion.Estado != EstadoAsignacionEvaluacion.ReasignadaPorConflicto
                && asignacion.Estado != EstadoAsignacionEvaluacion.Completada);

    internal IQueryable<AsignacionEvaluacion> ConsultarAsignacionesVigentesSinResultadoConsolidado(Guid convocatoriaId)
        => _contexto.AsignacionesEvaluacion
            .AsNoTracking()
            .Where(asignacion => asignacion.ConvocatoriaId == convocatoriaId
                && asignacion.Estado != EstadoAsignacionEvaluacion.ReasignadaPorConflicto
                && !_contexto.ResultadosConsolidadosRanking.Any(
                    resultado => resultado.ConvocatoriaId == convocatoriaId
                        && resultado.PostulacionId == asignacion.PostulacionId));

    private static void ValidarConvocatoriaId(Guid convocatoriaId)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }
    }
}
