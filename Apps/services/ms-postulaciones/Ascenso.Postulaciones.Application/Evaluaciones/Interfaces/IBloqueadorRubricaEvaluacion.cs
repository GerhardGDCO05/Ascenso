namespace Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;

/// <summary>
/// Puerto de aplicación que dispara el bloqueo automático de la rúbrica de evaluación
/// al registrarse la primera calificación (RN-POS-05).
/// </summary>
public interface IBloqueadorRubricaEvaluacion
{
    Task BloquearPorPrimeraCalificacionAsync(
        Guid rubricaId,
        CancellationToken cancellationToken = default);
}
