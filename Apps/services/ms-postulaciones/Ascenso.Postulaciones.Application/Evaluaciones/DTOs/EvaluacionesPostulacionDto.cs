namespace Ascenso.Postulaciones.Application.Evaluaciones.DTOs;

public sealed record class EvaluacionesPostulacionDto(
    Guid PostulacionId,
    IReadOnlyList<EvaluacionSupervisionDto> Evaluaciones);
