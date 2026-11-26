namespace Ascenso.Postulaciones.Application.Comites.DTOs;

public sealed record class MiembroComiteDto(
    Guid Id,
    Guid UsuarioEvaluadorId,
    string Tipo,
    string Rol,
    DateOnly FechaAsignacion,
    DateOnly? FechaDesvinculacion,
    bool Activo);
