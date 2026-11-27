namespace Ascenso.Postulaciones.Application.Consolidacion.DTOs;

public sealed record class NotaEvaluacionIndividualDto(
    Guid EvaluacionId,
    Guid AsignacionEvaluacionId,
    Guid EvaluadorId,
    decimal PuntajeTotalPonderado,
    DateTimeOffset FechaEnvio,
    string? ComentariosGenerales);
