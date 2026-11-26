namespace Ascenso.Postulaciones.Application.Asignaciones.DTOs;

public sealed record ExcepcionEvaluadorUnicoDto(
    Guid Id,
    Guid PostulacionId,
    string Justificacion,
    Guid UsuarioAutorizadorId,
    DateTimeOffset FechaAutorizacion);
