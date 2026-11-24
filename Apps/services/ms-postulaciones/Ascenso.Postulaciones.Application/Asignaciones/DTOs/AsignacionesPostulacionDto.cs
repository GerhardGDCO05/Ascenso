namespace Ascenso.Postulaciones.Application.Asignaciones.DTOs;

public sealed record class AsignacionesPostulacionDto(
    Guid PostulacionId,
    IReadOnlyList<AsignacionEvaluacionDto> Asignaciones);
