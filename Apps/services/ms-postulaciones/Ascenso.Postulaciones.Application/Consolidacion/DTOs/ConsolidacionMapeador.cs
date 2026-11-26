using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;

namespace Ascenso.Postulaciones.Application.Consolidacion.DTOs;

public static class ConsolidacionMapeador
{
    public static ResultadoConsolidadoDto ADto(this ResultadoConsolidadoRanking resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        return new ResultadoConsolidadoDto(
            resultado.Id,
            resultado.ConvocatoriaId,
            resultado.PostulacionId,
            resultado.ExcepcionEvaluadorUnico,
            resultado.JustificacionEvaluadorUnico,
            resultado.AutorizadorExcepcionId,
            resultado.CantidadEvaluacionesValidas,
            resultado.PuntajeConsolidado,
            resultado.DiferenciaEntreEvaluadores,
            resultado.UmbralDiscrepanciaAplicado,
            resultado.AlertaDiscrepancia,
            resultado.DiscrepanciaRevisada,
            resultado.ComentarioRevisionDiscrepancia,
            resultado.RevisadoPorUsuarioId,
            resultado.FechaRevisionDiscrepancia,
            resultado.EstadoConfirmacionRanking.ACodigo(),
            resultado.FechaConsolidacion,
            resultado.Version,
            resultado.PosicionRanking,
            resultado.MontoEstimadoBeca,
            resultado.Clasificacion?.ACodigo(),
            resultado.FechaConfirmacionRanking,
            resultado.ConfirmadoPorUsuarioId);
    }

    public static NotaEvaluacionIndividualDto ANotaIndividualDto(this Evaluacion evaluacion)
    {
        ArgumentNullException.ThrowIfNull(evaluacion);

        if (evaluacion.Estado != EstadoEvaluacion.EnviadaDefinitiva
            || !evaluacion.FechaEnvio.HasValue
            || !evaluacion.PuntajeTotalPonderado.HasValue)
        {
            throw new InvalidOperationException(
                "Solo se pueden mapear notas individuales de evaluaciones en estado ENVIADA_DEFINITIVA con fecha de envío y puntaje definitivo.");
        }

        return new NotaEvaluacionIndividualDto(
            evaluacion.Id,
            evaluacion.AsignacionEvaluacionId,
            evaluacion.EvaluadorId,
            evaluacion.PuntajeTotalPonderado.Value,
            evaluacion.FechaEnvio.Value,
            evaluacion.ComentariosGenerales);
    }

    public static DetalleResultadoConsolidadoDto ADetalleDto(
        this ResultadoConsolidadoRanking resultado,
        IEnumerable<Evaluacion> evaluaciones)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        ArgumentNullException.ThrowIfNull(evaluaciones);

        var notasOrdenadas = evaluaciones
            .Where(evaluacion => evaluacion is not null
                && evaluacion.Estado == EstadoEvaluacion.EnviadaDefinitiva
                && evaluacion.FechaEnvio.HasValue
                && evaluacion.PuntajeTotalPonderado.HasValue)
            .OrderBy(evaluacion => evaluacion.FechaEnvio!.Value)
            .ThenBy(evaluacion => evaluacion.Id)
            .Select(evaluacion => evaluacion.ANotaIndividualDto())
            .ToArray();

        return new DetalleResultadoConsolidadoDto(
            resultado.Id,
            resultado.ConvocatoriaId,
            resultado.PostulacionId,
            resultado.ExcepcionEvaluadorUnico,
            resultado.JustificacionEvaluadorUnico,
            resultado.AutorizadorExcepcionId,
            resultado.CantidadEvaluacionesValidas,
            resultado.PuntajeConsolidado,
            resultado.DiferenciaEntreEvaluadores,
            resultado.UmbralDiscrepanciaAplicado,
            resultado.AlertaDiscrepancia,
            resultado.DiscrepanciaRevisada,
            resultado.ComentarioRevisionDiscrepancia,
            resultado.RevisadoPorUsuarioId,
            resultado.FechaRevisionDiscrepancia,
            resultado.EstadoConfirmacionRanking.ACodigo(),
            resultado.FechaConsolidacion,
            resultado.Version,
            notasOrdenadas,
            resultado.PosicionRanking,
            resultado.MontoEstimadoBeca,
            resultado.Clasificacion?.ACodigo(),
            resultado.FechaConfirmacionRanking,
            resultado.ConfirmadoPorUsuarioId);
    }
}
