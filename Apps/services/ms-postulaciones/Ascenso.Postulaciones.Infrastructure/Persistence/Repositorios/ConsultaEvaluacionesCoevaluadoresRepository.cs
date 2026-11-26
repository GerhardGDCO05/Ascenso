using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del puerto segregado de consulta de asignaciones vigentes y evaluaciones definitivas de coevaluadores.
/// </summary>
public sealed class ConsultaEvaluacionesCoevaluadoresRepository : IConsultaEvaluacionesCoevaluadoresRepository
{
    private readonly PostulacionesDbContext _contexto;

    public ConsultaEvaluacionesCoevaluadoresRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<AsignacionEvaluacion?> ObtenerAsignacionVigenteEvaluadorAsync(
        Guid postulacionId,
        Guid usuarioEvaluadorId,
        CancellationToken cancellationToken = default)
    {
        ValidarArgumentos(postulacionId, usuarioEvaluadorId);

        return ConsultarAsignacionVigenteEvaluador(postulacionId, usuarioEvaluadorId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<AsignacionEvaluacion> ConsultarAsignacionVigenteEvaluador(
        Guid postulacionId,
        Guid usuarioEvaluadorId)
        => _contexto.AsignacionesEvaluacion
            .AsNoTracking()
            .Where(asignacion => asignacion.PostulacionId == postulacionId
                && asignacion.UsuarioEvaluadorId == usuarioEvaluadorId
                && asignacion.Estado != EstadoAsignacionEvaluacion.ReasignadaPorConflicto);

    public async Task<IReadOnlyList<Evaluacion>> ListarDefinitivasDeCoevaluadoresAsync(
        Guid postulacionId,
        Guid usuarioEvaluadorId,
        CancellationToken cancellationToken = default)
    {
        ValidarArgumentos(postulacionId, usuarioEvaluadorId);

        return await ConsultarDefinitivasDeCoevaluadores(postulacionId, usuarioEvaluadorId)
            .ToListAsync(cancellationToken);
    }

    internal IQueryable<Evaluacion> ConsultarDefinitivasDeCoevaluadores(
        Guid postulacionId,
        Guid usuarioEvaluadorId)
        => _contexto.Evaluaciones
            .AsNoTracking()
            .Include(evaluacion => evaluacion.Calificaciones)
            .Where(evaluacion => evaluacion.PostulacionId == postulacionId
                && evaluacion.EvaluadorId != usuarioEvaluadorId
                && evaluacion.Estado == EstadoEvaluacion.EnviadaDefinitiva)
            .OrderBy(evaluacion => evaluacion.FechaEnvio)
            .ThenBy(evaluacion => evaluacion.Id);

    private static void ValidarArgumentos(Guid postulacionId, Guid usuarioEvaluadorId)
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }

        if (usuarioEvaluadorId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario evaluador es obligatorio.",
                nameof(usuarioEvaluadorId));
        }
    }
}
