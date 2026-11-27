using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.Entidades;

namespace Ascenso.Postulaciones.Application.Evaluaciones.DTOs;

public static class EvaluacionMapeador
{
    public static CalificacionCriterioDto ADto(this CalificacionCriterio calificacion)
    {
        ArgumentNullException.ThrowIfNull(calificacion);

        return new CalificacionCriterioDto(
            calificacion.Id,
            calificacion.CriterioEvaluacionId,
            calificacion.PuntajeAsignado,
            calificacion.Comentario);
    }

    public static EvaluacionDto ADto(this Evaluacion evaluacion)
    {
        ArgumentNullException.ThrowIfNull(evaluacion);

        return new EvaluacionDto(
            evaluacion.Id,
            evaluacion.AsignacionEvaluacionId,
            evaluacion.PostulacionId,
            evaluacion.ConvocatoriaId,
            evaluacion.RubricaEvaluacionId,
            evaluacion.Estado.ACodigo(),
            evaluacion.FechaInicio,
            evaluacion.FechaEnvio,
            evaluacion.PuntajeTotalPonderado,
            evaluacion.ComentariosGenerales,
            MapearCalificacionesOrdenadas(evaluacion),
            evaluacion.Version);
    }

    public static ResumenMiEvaluacionDto AResumenMiEvaluacionDto(this Evaluacion evaluacion)
    {
        ArgumentNullException.ThrowIfNull(evaluacion);

        return new ResumenMiEvaluacionDto(
            evaluacion.Id,
            evaluacion.AsignacionEvaluacionId,
            evaluacion.PostulacionId,
            evaluacion.ConvocatoriaId,
            evaluacion.RubricaEvaluacionId,
            evaluacion.Estado.ACodigo(),
            evaluacion.FechaInicio,
            evaluacion.FechaEnvio,
            evaluacion.PuntajeTotalPonderado,
            evaluacion.Calificaciones.Count,
            evaluacion.Version);
    }

    public static EvaluacionSupervisionDto ASupervisionDto(this Evaluacion evaluacion)
    {
        ArgumentNullException.ThrowIfNull(evaluacion);

        return new EvaluacionSupervisionDto(
            evaluacion.Id,
            evaluacion.AsignacionEvaluacionId,
            evaluacion.PostulacionId,
            evaluacion.ConvocatoriaId,
            evaluacion.RubricaEvaluacionId,
            evaluacion.EvaluadorId,
            evaluacion.Estado.ACodigo(),
            evaluacion.FechaInicio,
            evaluacion.FechaEnvio,
            evaluacion.PuntajeTotalPonderado,
            evaluacion.ComentariosGenerales,
            evaluacion.MotivoInvalidacion,
            MapearCalificacionesOrdenadas(evaluacion),
            evaluacion.Version);
    }

    public static EvaluacionCoevaluadorDto ACoevaluadorDto(this Evaluacion evaluacion)
    {
        ArgumentNullException.ThrowIfNull(evaluacion);

        if (evaluacion.Estado != EstadoEvaluacion.EnviadaDefinitiva
            || !evaluacion.FechaEnvio.HasValue
            || !evaluacion.PuntajeTotalPonderado.HasValue)
        {
            throw new InvalidOperationException(
                "Solo se pueden mapear evaluaciones en estado ENVIADA_DEFINITIVA con fecha de envío y puntaje definitivo.");
        }

        return new EvaluacionCoevaluadorDto(
            evaluacion.Id,
            evaluacion.RubricaEvaluacionId,
            evaluacion.Estado.ACodigo(),
            evaluacion.FechaEnvio.Value,
            evaluacion.PuntajeTotalPonderado.Value,
            evaluacion.ComentariosGenerales,
            MapearCalificacionesOrdenadas(evaluacion));
    }

    private static CalificacionCriterioDto[] MapearCalificacionesOrdenadas(Evaluacion evaluacion)
        => evaluacion.Calificaciones
            .OrderBy(calificacion => calificacion.CriterioEvaluacionId)
            .ThenBy(calificacion => calificacion.Id)
            .Select(calificacion => calificacion.ADto())
            .ToArray();
}
