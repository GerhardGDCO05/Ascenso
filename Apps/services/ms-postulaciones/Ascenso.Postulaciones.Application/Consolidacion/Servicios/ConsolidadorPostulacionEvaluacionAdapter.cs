using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConsolidarPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Interfaces;

namespace Ascenso.Postulaciones.Application.Consolidacion.Servicios;

public sealed class ConsolidadorPostulacionEvaluacionAdapter : IConsolidadorPostulacionEvaluacion
{
    private readonly ConsolidarPostulacionCommandHandler _consolidarPostulacionHandler;

    public ConsolidadorPostulacionEvaluacionAdapter(
        ConsolidarPostulacionCommandHandler consolidarPostulacionHandler)
    {
        _consolidarPostulacionHandler = consolidarPostulacionHandler
            ?? throw new ArgumentNullException(nameof(consolidarPostulacionHandler));
    }

    public async Task IntentarConsolidarAutomaticamenteAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        await _consolidarPostulacionHandler.IntentarConsolidarAutomaticamenteAsync(
            postulacionId,
            cancellationToken);
    }
}
