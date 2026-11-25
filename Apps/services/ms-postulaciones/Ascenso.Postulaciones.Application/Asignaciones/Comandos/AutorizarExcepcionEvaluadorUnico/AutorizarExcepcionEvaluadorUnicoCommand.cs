namespace Ascenso.Postulaciones.Application.Asignaciones.Comandos.AutorizarExcepcionEvaluadorUnico;

public sealed record AutorizarExcepcionEvaluadorUnicoCommand(
    Guid PostulacionId,
    string Justificacion);
