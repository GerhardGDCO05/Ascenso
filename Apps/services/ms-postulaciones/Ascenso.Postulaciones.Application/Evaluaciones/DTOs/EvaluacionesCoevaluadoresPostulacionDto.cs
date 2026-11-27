namespace Ascenso.Postulaciones.Application.Evaluaciones.DTOs;

public sealed record class EvaluacionesCoevaluadoresPostulacionDto(
    Guid PostulacionId,
    Guid ConvocatoriaId,
    DateTimeOffset FechaCierreRevision,
    IReadOnlyList<EvaluacionCoevaluadorDto> Evaluaciones);
