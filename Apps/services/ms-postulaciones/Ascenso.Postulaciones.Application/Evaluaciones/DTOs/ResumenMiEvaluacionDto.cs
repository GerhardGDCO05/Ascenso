namespace Ascenso.Postulaciones.Application.Evaluaciones.DTOs;

public sealed record class ResumenMiEvaluacionDto(
    Guid EvaluacionId,
    Guid AsignacionEvaluacionId,
    Guid PostulacionId,
    Guid ConvocatoriaId,
    Guid RubricaEvaluacionId,
    string Estado,
    DateTimeOffset FechaInicio,
    DateTimeOffset? FechaEnvio,
    decimal? PuntajeTotalPonderado,
    int CantidadCriteriosCalificados,
    int Version);
