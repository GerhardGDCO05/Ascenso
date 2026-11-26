namespace Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;

public sealed record class DatosCalificacionCriterioCommand(
    Guid CriterioEvaluacionId,
    decimal PuntajeAsignado,
    string? Comentario = null);

public sealed record class GuardarBorradorEvaluacionCommand(
    Guid AsignacionEvaluacionId,
    IReadOnlyList<DatosCalificacionCriterioCommand> Calificaciones,
    string? ComentariosGenerales = null);
