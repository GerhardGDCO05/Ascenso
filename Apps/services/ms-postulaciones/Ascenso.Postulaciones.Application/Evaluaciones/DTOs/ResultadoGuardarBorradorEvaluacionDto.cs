namespace Ascenso.Postulaciones.Application.Evaluaciones.DTOs;

public sealed record class ResultadoGuardarBorradorEvaluacionDto(
    EvaluacionDto Evaluacion,
    bool EsCreacion);
