using Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;
using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Servicios;

public sealed class BloqueadorRubricaEvaluacionAdapter : IBloqueadorRubricaEvaluacion
{
    private readonly BloquearRubricaCommandHandler _bloquearRubricaHandler;

    public BloqueadorRubricaEvaluacionAdapter(BloquearRubricaCommandHandler bloquearRubricaHandler)
    {
        _bloquearRubricaHandler = bloquearRubricaHandler
            ?? throw new ArgumentNullException(nameof(bloquearRubricaHandler));
    }

    public async Task BloquearPorPrimeraCalificacionAsync(
        Guid rubricaId,
        CancellationToken cancellationToken = default)
    {
        await _bloquearRubricaHandler.HandleAsync(
            new BloquearRubricaCommand(rubricaId, MotivoBloqueoRubrica.PrimeraCalificacion),
            cancellationToken);
    }
}
