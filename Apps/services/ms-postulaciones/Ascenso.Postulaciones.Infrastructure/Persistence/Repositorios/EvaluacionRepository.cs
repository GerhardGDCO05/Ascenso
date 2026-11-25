using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de evaluaciones de postulaciones.
/// </summary>
public sealed class EvaluacionRepository : IEvaluacionRepository
{
    public const int TamanoMaximoPagina = 100;

    private readonly PostulacionesDbContext _contexto;

    public EvaluacionRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<Evaluacion?> ObtenerPorAsignacionAsync(
        Guid asignacionEvaluacionId,
        CancellationToken cancellationToken = default)
    {
        if (asignacionEvaluacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la asignación de evaluación es obligatorio.",
                nameof(asignacionEvaluacionId));
        }

        return ConsultarPorAsignacion(asignacionEvaluacionId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<Evaluacion> ConsultarPorAsignacion(Guid asignacionEvaluacionId)
        => _contexto.Evaluaciones
            .AsNoTracking()
            .Include(evaluacion => evaluacion.Calificaciones)
            .Where(evaluacion => evaluacion.AsignacionEvaluacionId == asignacionEvaluacionId);

    public Task<Evaluacion?> ObtenerParaActualizarPorAsignacionAsync(
        Guid asignacionEvaluacionId,
        CancellationToken cancellationToken = default)
    {
        if (asignacionEvaluacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la asignación de evaluación es obligatorio.",
                nameof(asignacionEvaluacionId));
        }

        return ConsultarParaActualizarPorAsignacion(asignacionEvaluacionId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<Evaluacion> ConsultarParaActualizarPorAsignacion(Guid asignacionEvaluacionId)
        => _contexto.Evaluaciones
            .Include(evaluacion => evaluacion.Calificaciones)
            .Where(evaluacion => evaluacion.AsignacionEvaluacionId == asignacionEvaluacionId);

    public async Task<PaginaResultado<Evaluacion>> ListarPorEvaluadorAsync(
        Guid evaluadorId,
        EstadoEvaluacion? estado,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        ValidarConsultaPorEvaluador(evaluadorId, estado, pagina, tamano);

        var consultaBase = ConsultarPorEvaluador(evaluadorId, estado);
        var total = await consultaBase.CountAsync(cancellationToken);
        var items = await ConsultarPaginaPorEvaluador(evaluadorId, estado, pagina, tamano)
            .ToListAsync(cancellationToken);

        return new PaginaResultado<Evaluacion>(items, total);
    }

    internal IQueryable<Evaluacion> ConsultarPorEvaluador(
        Guid evaluadorId,
        EstadoEvaluacion? estado)
    {
        var consulta = _contexto.Evaluaciones
            .AsNoTracking()
            .Where(evaluacion => evaluacion.EvaluadorId == evaluadorId);

        if (estado.HasValue)
        {
            var estadoFiltro = estado.Value;
            consulta = consulta.Where(evaluacion => evaluacion.Estado == estadoFiltro);
        }

        return consulta;
    }

    internal IQueryable<Evaluacion> ConsultarPaginaPorEvaluador(
        Guid evaluadorId,
        EstadoEvaluacion? estado,
        int pagina,
        int tamano)
        => ConsultarPorEvaluador(evaluadorId, estado)
            .Include(evaluacion => evaluacion.Calificaciones)
            .OrderByDescending(evaluacion => evaluacion.FechaUltimaModificacion)
            .ThenBy(evaluacion => evaluacion.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano);

    public async Task<IReadOnlyList<Evaluacion>> ListarDefinitivasPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }

        return await ConsultarDefinitivasPorPostulacion(postulacionId)
            .ToListAsync(cancellationToken);
    }

    internal IQueryable<Evaluacion> ConsultarDefinitivasPorPostulacion(Guid postulacionId)
        => _contexto.Evaluaciones
            .AsNoTracking()
            .Include(evaluacion => evaluacion.Calificaciones)
            .Where(evaluacion => evaluacion.PostulacionId == postulacionId
                && evaluacion.Estado != EstadoEvaluacion.Borrador)
            .OrderBy(evaluacion => evaluacion.FechaEnvio)
            .ThenBy(evaluacion => evaluacion.Id);

    public async Task AgregarAsync(
        Evaluacion evaluacion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluacion);
        await _contexto.Evaluaciones.AddAsync(evaluacion, cancellationToken);
    }

    private static void ValidarConsultaPorEvaluador(
        Guid evaluadorId,
        EstadoEvaluacion? estado,
        int pagina,
        int tamano)
    {
        if (evaluadorId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario evaluador es obligatorio.",
                nameof(evaluadorId));
        }

        if (estado.HasValue && !Enum.IsDefined(estado.Value))
        {
            throw new ArgumentException(
                "El estado de evaluación especificado no es válido.",
                nameof(estado));
        }

        if (pagina < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pagina),
                "El número de página debe ser mayor o igual a 1.");
        }

        if (tamano is < 1 or > TamanoMaximoPagina)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tamano),
                $"El tamaño de página debe estar entre 1 y {TamanoMaximoPagina}.");
        }
    }
}
