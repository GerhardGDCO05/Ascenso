using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de asignaciones de evaluación.
/// </summary>
public sealed class AsignacionEvaluacionRepository : IAsignacionEvaluacionRepository
{
    public const int TamanoMaximoPagina = 100;

    private readonly PostulacionesDbContext _contexto;

    public AsignacionEvaluacionRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task AgregarRangoAsync(
        IReadOnlyCollection<AsignacionEvaluacion> asignaciones,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asignaciones);

        if (asignaciones.Count == 0)
        {
            throw new ArgumentException(
                "Se debe proporcionar al menos una asignación de evaluación.",
                nameof(asignaciones));
        }

        await _contexto.AsignacionesEvaluacion.AddRangeAsync(asignaciones, cancellationToken);
    }

    public async Task<IReadOnlyList<AsignacionEvaluacion>> ListarVigentesPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }

        return await ConsultarVigentesPorPostulacion(postulacionId)
            .ToListAsync(cancellationToken);
    }

    internal IQueryable<AsignacionEvaluacion> ConsultarVigentesPorPostulacion(Guid postulacionId)
        => _contexto.AsignacionesEvaluacion
            .AsNoTracking()
            .Where(asignacion => asignacion.PostulacionId == postulacionId
                && asignacion.Estado != EstadoAsignacionEvaluacion.ReasignadaPorConflicto);

    public async Task<IReadOnlyList<AsignacionEvaluacion>> ListarPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }

        return await ConsultarPorPostulacion(postulacionId)
            .ToListAsync(cancellationToken);
    }

    internal IQueryable<AsignacionEvaluacion> ConsultarPorPostulacion(Guid postulacionId)
        => _contexto.AsignacionesEvaluacion
            .AsNoTracking()
            .Where(asignacion => asignacion.PostulacionId == postulacionId)
            .OrderBy(asignacion => asignacion.FechaAsignacion)
            .ThenBy(asignacion => asignacion.Id);

    public async Task<IReadOnlyDictionary<Guid, int>> ObtenerCargaPorMiembroAsync(
        Guid comiteEvaluadorId,
        CancellationToken cancellationToken = default)
    {
        if (comiteEvaluadorId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del comité evaluador es obligatorio.",
                nameof(comiteEvaluadorId));
        }

        return await ConsultarAbiertasPorComite(comiteEvaluadorId)
            .GroupBy(asignacion => asignacion.MiembroComiteId)
            .Select(grupo => new
            {
                MiembroComiteId = grupo.Key,
                Cantidad = grupo.Count()
            })
            .ToDictionaryAsync(
                item => item.MiembroComiteId,
                item => item.Cantidad,
                cancellationToken);
    }

    internal IQueryable<AsignacionEvaluacion> ConsultarAbiertasPorComite(Guid comiteEvaluadorId)
        => _contexto.AsignacionesEvaluacion
            .AsNoTracking()
            .Where(asignacion => asignacion.ComiteEvaluadorId == comiteEvaluadorId
                && (asignacion.Estado == EstadoAsignacionEvaluacion.Pendiente
                    || asignacion.Estado == EstadoAsignacionEvaluacion.EnProgreso));

    public async Task<PaginaResultado<AsignacionEvaluacion>> ListarPorEvaluadorAsync(
        Guid usuarioEvaluadorId,
        EstadoAsignacionEvaluacion? estado,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        ValidarConsultaPorEvaluador(usuarioEvaluadorId, estado, pagina, tamano);

        var consultaBase = ConsultarPorEvaluador(usuarioEvaluadorId, estado);
        var total = await consultaBase.CountAsync(cancellationToken);
        var items = await ConsultarPaginaPorEvaluador(usuarioEvaluadorId, estado, pagina, tamano)
            .ToListAsync(cancellationToken);

        return new PaginaResultado<AsignacionEvaluacion>(items, total);
    }

    internal IQueryable<AsignacionEvaluacion> ConsultarPorEvaluador(
        Guid usuarioEvaluadorId,
        EstadoAsignacionEvaluacion? estado)
    {
        var consulta = _contexto.AsignacionesEvaluacion
            .AsNoTracking()
            .Where(asignacion => asignacion.UsuarioEvaluadorId == usuarioEvaluadorId);

        if (estado.HasValue)
        {
            var estadoFiltro = estado.Value;
            consulta = consulta.Where(asignacion => asignacion.Estado == estadoFiltro);
        }

        return consulta;
    }

    internal IQueryable<AsignacionEvaluacion> ConsultarPaginaPorEvaluador(
        Guid usuarioEvaluadorId,
        EstadoAsignacionEvaluacion? estado,
        int pagina,
        int tamano)
        => ConsultarPorEvaluador(usuarioEvaluadorId, estado)
            .OrderBy(asignacion => asignacion.FechaLimiteEvaluacion)
            .ThenBy(asignacion => asignacion.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano);

    private static void ValidarConsultaPorEvaluador(
        Guid usuarioEvaluadorId,
        EstadoAsignacionEvaluacion? estado,
        int pagina,
        int tamano)
    {
        if (usuarioEvaluadorId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario evaluador es obligatorio.",
                nameof(usuarioEvaluadorId));
        }

        if (estado.HasValue && !Enum.IsDefined(estado.Value))
        {
            throw new ArgumentException(
                "El estado de asignación especificado no es válido.",
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
