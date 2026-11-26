using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de consulta y carga rastreada de resultados consolidados de una convocatoria para el cálculo y confirmación de ranking.
/// </summary>
public interface IRankingConvocatoriaRepository
{
    Task<IReadOnlyList<ResultadoConsolidadoRanking>> ListarPorConvocatoriaAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResultadoConsolidadoRanking>> ListarParaConfirmarPorConvocatoriaAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);
}
