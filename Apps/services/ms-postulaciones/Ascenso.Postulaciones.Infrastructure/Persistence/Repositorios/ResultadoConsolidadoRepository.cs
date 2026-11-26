using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de resultados consolidados de ranking.
/// </summary>
public sealed class ResultadoConsolidadoRepository : IResultadoConsolidadoRepository
{
    public const int TamanoMaximoPagina = 100;

    private readonly PostulacionesDbContext _contexto;

    public ResultadoConsolidadoRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<ResultadoConsolidadoRanking?> ObtenerPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        ValidarPostulacionId(postulacionId);

        return ConsultarPorPostulacion(postulacionId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<ResultadoConsolidadoRanking> ConsultarPorPostulacion(Guid postulacionId)
        => _contexto.ResultadosConsolidadosRanking
            .AsNoTracking()
            .Where(resultado => resultado.PostulacionId == postulacionId);

    public Task<ResultadoConsolidadoRanking?> ObtenerParaActualizarPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        ValidarPostulacionId(postulacionId);

        return ConsultarParaActualizarPorPostulacion(postulacionId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<ResultadoConsolidadoRanking> ConsultarParaActualizarPorPostulacion(Guid postulacionId)
        => _contexto.ResultadosConsolidadosRanking
            .Where(resultado => resultado.PostulacionId == postulacionId);

    public Task<ResultadoConsolidadoRanking?> RecargarParaActualizarPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        ValidarPostulacionId(postulacionId);

        DescartarRastreoLocalPorPostulacion(postulacionId);

        return ConsultarParaActualizarPorPostulacion(postulacionId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal void DescartarRastreoLocalPorPostulacion(Guid postulacionId)
    {
        var entradasRastreadas = _contexto.ChangeTracker
            .Entries<ResultadoConsolidadoRanking>()
            .Where(entrada => entrada.Entity.PostulacionId == postulacionId)
            .ToList();

        foreach (var entrada in entradasRastreadas)
        {
            entrada.State = EntityState.Detached;
        }
    }

    public async Task<PaginaResultado<ResultadoConsolidadoRanking>> ListarPorConvocatoriaAsync(
        Guid convocatoriaId,
        bool? alertaDiscrepancia,
        bool? discrepanciaRevisada,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        ValidarConsultaPorConvocatoria(convocatoriaId, pagina, tamano);

        var consultaBase = ConsultarPorConvocatoria(convocatoriaId, alertaDiscrepancia, discrepanciaRevisada);
        var total = await consultaBase.CountAsync(cancellationToken);
        var items = await ConsultarPaginaPorConvocatoria(
            convocatoriaId,
            alertaDiscrepancia,
            discrepanciaRevisada,
            pagina,
            tamano)
            .ToListAsync(cancellationToken);

        return new PaginaResultado<ResultadoConsolidadoRanking>(items, total);
    }

    internal IQueryable<ResultadoConsolidadoRanking> ConsultarPorConvocatoria(
        Guid convocatoriaId,
        bool? alertaDiscrepancia,
        bool? discrepanciaRevisada)
    {
        var consulta = _contexto.ResultadosConsolidadosRanking
            .AsNoTracking()
            .Where(resultado => resultado.ConvocatoriaId == convocatoriaId);

        if (alertaDiscrepancia.HasValue)
        {
            var alertaFiltro = alertaDiscrepancia.Value;
            consulta = consulta.Where(resultado => resultado.AlertaDiscrepancia == alertaFiltro);
        }

        if (discrepanciaRevisada.HasValue)
        {
            var revisadaFiltro = discrepanciaRevisada.Value;
            consulta = consulta.Where(resultado => resultado.DiscrepanciaRevisada == revisadaFiltro);
        }

        return consulta;
    }

    internal IQueryable<ResultadoConsolidadoRanking> ConsultarPaginaPorConvocatoria(
        Guid convocatoriaId,
        bool? alertaDiscrepancia,
        bool? discrepanciaRevisada,
        int pagina,
        int tamano)
        => ConsultarPorConvocatoria(convocatoriaId, alertaDiscrepancia, discrepanciaRevisada)
            .OrderByDescending(resultado => resultado.PuntajeConsolidado)
            .ThenBy(resultado => resultado.FechaConsolidacion)
            .ThenBy(resultado => resultado.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano);

    public async Task AgregarAsync(
        ResultadoConsolidadoRanking resultado,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        await _contexto.ResultadosConsolidadosRanking.AddAsync(resultado, cancellationToken);
    }

    private static void ValidarPostulacionId(Guid postulacionId)
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }
    }

    private static void ValidarConsultaPorConvocatoria(
        Guid convocatoriaId,
        int pagina,
        int tamano)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
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
