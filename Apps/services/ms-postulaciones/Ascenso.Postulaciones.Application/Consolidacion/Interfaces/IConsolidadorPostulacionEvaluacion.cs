namespace Ascenso.Postulaciones.Application.Consolidacion.Interfaces;

/// <summary>
/// Puerto de aplicación que dispara la consolidación automática e idempotente de una postulación
/// al completarse evaluaciones definitivas o autorizarse una excepción de evaluador único.
/// </summary>
public interface IConsolidadorPostulacionEvaluacion
{
    Task IntentarConsolidarAutomaticamenteAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);
}
