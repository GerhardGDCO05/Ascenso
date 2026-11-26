using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.SeguimientoAcademico.Entidades;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Dtos;

public sealed record CondicionPermanenciaEvaluadaDto(
    TipoCondicionPermanencia Condicion, string ValorRequerido,
    string ValorObtenido, bool Cumple);

public sealed record EvaluacionPermanenciaDto(
    Guid Id, Guid ReporteAcademicoId, Guid BecaId,
    DateTimeOffset FechaEvaluacion, bool CumpleCondiciones,
    IReadOnlyList<CondicionPermanenciaEvaluadaDto> DetalleCondiciones,
    ConsecuenciaPermanencia? ConsecuenciaRecomendada,
    ConsecuenciaPermanencia? ConsecuenciaAplicada,
    EstadoConsecuencia EstadoConfirmacion,
    string? MotivoDecision, string? SiguientePasoBecario,
    Guid? EvaluadoPorCoordinadorId)
{
    public static EvaluacionPermanenciaDto Desde(EvaluacionPermanencia evaluacion)
        => new(evaluacion.Id, evaluacion.ReporteAcademicoId, evaluacion.BecaId,
            evaluacion.FechaEvaluacion, evaluacion.CumpleCondiciones,
            evaluacion.DetalleCondiciones.Select(detalle =>
                new CondicionPermanenciaEvaluadaDto(detalle.Condicion,
                    detalle.ValorRequerido, detalle.ValorObtenido,
                    detalle.Cumple)).ToArray(),
            evaluacion.ConsecuenciaRecomendada,
            evaluacion.ConsecuenciaAplicada,
            evaluacion.EstadoConfirmacion,
            evaluacion.MotivoDecision,
            evaluacion.SiguientePasoBecario,
            evaluacion.EvaluadoPorCoordinadorId);
}
