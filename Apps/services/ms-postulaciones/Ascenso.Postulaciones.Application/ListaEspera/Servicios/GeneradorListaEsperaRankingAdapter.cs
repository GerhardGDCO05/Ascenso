using Ascenso.Postulaciones.Application.ListaEspera.Interfaces;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Servicios;

namespace Ascenso.Postulaciones.Application.ListaEspera.Servicios;

/// <summary>
/// Adaptador de aplicación que construye y encola las entradas de lista de espera
/// sin confirmar la transacción por separado, preservando atomicidad en la confirmación del ranking.
/// </summary>
public sealed class GeneradorListaEsperaRankingAdapter : IGeneradorListaEsperaRanking
{
    private readonly IEntradaListaEsperaRepository _entradaListaEsperaRepository;

    public GeneradorListaEsperaRankingAdapter(IEntradaListaEsperaRepository entradaListaEsperaRepository)
    {
        _entradaListaEsperaRepository = entradaListaEsperaRepository
            ?? throw new ArgumentNullException(nameof(entradaListaEsperaRepository));
    }

    public async Task<int> GenerarDesdeRankingConfirmadoAsync(
        Guid convocatoriaId,
        Guid programaBecaId,
        IReadOnlyCollection<ResultadoConsolidadoRanking> resultadosConfirmados,
        DateTimeOffset fechaIngreso,
        Guid usuarioActorId,
        CancellationToken cancellationToken = default)
    {
        var entradas = ServicioConstruccionListaEspera.ConstruirDesdeResultadosConfirmados(
            convocatoriaId,
            programaBecaId,
            resultadosConfirmados,
            fechaIngreso,
            usuarioActorId);

        if (entradas.Count == 0)
        {
            return 0;
        }

        await _entradaListaEsperaRepository.AgregarRangoAsync(entradas, cancellationToken);
        return entradas.Count;
    }
}
