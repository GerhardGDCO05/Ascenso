namespace Ascenso.Postulaciones.Application.Asignaciones.DTOs;

public sealed record class AsignacionEvaluacionDto(
    Guid Id,
    Guid ComiteEvaluadorId,
    Guid ConvocatoriaId,
    Guid PostulacionId,
    Guid MiembroComiteId,
    Guid UsuarioEvaluadorId,
    string Estado,
    DateTimeOffset FechaAsignacion,
    DateTimeOffset FechaLimiteEvaluacion,
    int Version);
