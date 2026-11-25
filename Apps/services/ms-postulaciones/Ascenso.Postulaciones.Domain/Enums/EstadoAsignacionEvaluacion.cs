namespace Ascenso.Postulaciones.Domain.Enums;

/// <summary>
/// Estado del ciclo de vida de una asignación de evaluación de postulación.
/// </summary>
public enum EstadoAsignacionEvaluacion
{
    Pendiente,
    EnProgreso,
    Completada,
    ReasignadaPorConflicto
}
