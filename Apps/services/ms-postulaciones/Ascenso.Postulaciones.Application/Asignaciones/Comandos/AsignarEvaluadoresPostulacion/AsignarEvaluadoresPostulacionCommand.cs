namespace Ascenso.Postulaciones.Application.Asignaciones.Comandos.AsignarEvaluadoresPostulacion;

public sealed record class AsignarEvaluadoresPostulacionCommand(
    Guid ComiteId,
    Guid PostulacionId,
    DateTimeOffset FechaLimiteEvaluacion,
    IReadOnlyCollection<Guid>? UsuariosEvaluadoresIds = null,
    int? CantidadEvaluadores = null);
