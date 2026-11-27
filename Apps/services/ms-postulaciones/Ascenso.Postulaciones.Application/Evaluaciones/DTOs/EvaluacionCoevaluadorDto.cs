namespace Ascenso.Postulaciones.Application.Evaluaciones.DTOs;

public sealed record class EvaluacionCoevaluadorDto(
    Guid EvaluacionId,
    Guid RubricaEvaluacionId,
    string Estado,
    DateTimeOffset FechaEnvio,
    decimal PuntajeTotalPonderado,
    string? ComentariosGenerales,
    IReadOnlyList<CalificacionCriterioDto> Calificaciones);
