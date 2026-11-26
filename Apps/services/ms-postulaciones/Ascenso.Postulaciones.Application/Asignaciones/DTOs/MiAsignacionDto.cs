namespace Ascenso.Postulaciones.Application.Asignaciones.DTOs;

/// <summary>
/// Vista ciega de una asignación propia para el evaluador (RN-POS-04).
/// No expone identidad del postulante ni de los coevaluadores.
/// </summary>
public sealed record class MiAsignacionDto(
    Guid AsignacionEvaluacionId,
    Guid PostulacionId,
    Guid ConvocatoriaId,
    string Estado,
    DateTimeOffset FechaAsignacion,
    DateTimeOffset FechaLimiteEvaluacion);
