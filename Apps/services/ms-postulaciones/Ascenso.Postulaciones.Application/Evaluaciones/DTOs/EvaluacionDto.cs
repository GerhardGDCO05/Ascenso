namespace Ascenso.Postulaciones.Application.Evaluaciones.DTOs;

public sealed record class EvaluacionDto(
    Guid Id,
    Guid AsignacionEvaluacionId,
    Guid PostulacionId,
    Guid ConvocatoriaId,
    Guid RubricaEvaluacionId,
    string Estado,
    DateTimeOffset FechaInicio,
    DateTimeOffset? FechaEnvio,
    decimal? PuntajeTotalPonderado,
    string? ComentariosGenerales,
    IReadOnlyList<CalificacionCriterioDto> Calificaciones,
    int Version);
