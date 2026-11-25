namespace Ascenso.Postulaciones.Application.Evaluaciones.DTOs;

public sealed record class EvaluacionSupervisionDto(
    Guid Id,
    Guid AsignacionEvaluacionId,
    Guid PostulacionId,
    Guid ConvocatoriaId,
    Guid RubricaEvaluacionId,
    Guid EvaluadorId,
    string Estado,
    DateTimeOffset FechaInicio,
    DateTimeOffset? FechaEnvio,
    decimal? PuntajeTotalPonderado,
    string? ComentariosGenerales,
    string? MotivoInvalidacion,
    IReadOnlyList<CalificacionCriterioDto> Calificaciones,
    int Version);
