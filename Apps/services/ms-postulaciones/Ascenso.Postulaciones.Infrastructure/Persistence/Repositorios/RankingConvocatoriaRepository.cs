using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del puerto de consulta y carga rastreada de resultados consolidados de una convocatoria para ranking.
/// </summary>
public sealed class RankingConvocatoriaRepository : IRankingConvocatoriaRepository
{
    private readonly PostulacionesDbContext _contexto;

    public RankingConvocatoriaRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<IReadOnlyList<ResultadoConsolidadoRanking>> ListarPorConvocatoriaAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        ValidarConvocatoriaId(convocatoriaId);

        return await ConsultarPorConvocatoria(convocatoriaId)
            .ToListAsync(cancellationToken);
    }

    internal IQueryable<ResultadoConsolidadoRanking> ConsultarPorConvocatoria(Guid convocatoriaId)
        => OrdenarResultadosRanking(
            _contexto.ResultadosConsolidadosRanking
                .AsNoTracking()
                .Where(resultado => resultado.ConvocatoriaId == convocatoriaId));

    public async Task<IReadOnlyList<ResultadoConsolidadoRanking>> ListarParaConfirmarPorConvocatoriaAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        ValidarConvocatoriaId(convocatoriaId);

        return await ConsultarParaConfirmarPorConvocatoria(convocatoriaId)
            .ToListAsync(cancellationToken);
    }

    internal IQueryable<ResultadoConsolidadoRanking> ConsultarParaConfirmarPorConvocatoria(Guid convocatoriaId)
        => OrdenarResultadosRanking(
            _contexto.ResultadosConsolidadosRanking
                .Where(resultado => resultado.ConvocatoriaId == convocatoriaId));

    private static IOrderedQueryable<ResultadoConsolidadoRanking> OrdenarResultadosRanking(
        IQueryable<ResultadoConsolidadoRanking> consulta)
        => consulta
            .OrderBy(resultado => resultado.PosicionRanking)
            .ThenByDescending(resultado => resultado.PuntajeConsolidado)
            .ThenBy(resultado => resultado.FechaConsolidacion)
            .ThenBy(resultado => resultado.Id);

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
