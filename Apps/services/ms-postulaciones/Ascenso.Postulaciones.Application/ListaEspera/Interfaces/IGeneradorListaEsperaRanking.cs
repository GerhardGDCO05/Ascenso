using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;

namespace Ascenso.Postulaciones.Application.ListaEspera.Interfaces;

/// <summary>
/// Puerto OCP de aplicación que materializa la cola ordenada de lista de espera
/// a partir de los resultados confirmados del ranking dentro de la misma unidad de trabajo.
/// </summary>
public interface IGeneradorListaEsperaRanking
{
    Task<int> GenerarDesdeRankingConfirmadoAsync(
        Guid convocatoriaId,
        Guid programaBecaId,
        IReadOnlyCollection<ResultadoConsolidadoRanking> resultadosConfirmados,
        DateTimeOffset fechaIngreso,
        Guid usuarioActorId,
        CancellationToken cancellationToken = default);
}
