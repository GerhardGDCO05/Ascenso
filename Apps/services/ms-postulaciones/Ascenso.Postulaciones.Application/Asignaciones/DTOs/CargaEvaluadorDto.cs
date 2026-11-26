namespace Ascenso.Postulaciones.Application.Asignaciones.DTOs;

public sealed record class CargaEvaluadorDto(
    Guid MiembroComiteId,
    Guid UsuarioEvaluadorId,
    string Tipo,
    string Rol,
    int AsignacionesAbiertas);
