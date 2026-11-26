namespace Ascenso.Postulaciones.Application.Evaluaciones.DTOs;

public sealed record class CalificacionCriterioDto(
    Guid Id,
    Guid CriterioEvaluacionId,
    decimal PuntajeAsignado,
    string? Comentario);
