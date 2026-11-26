using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Consultas.ObtenerEvaluacionPermanencia;

public sealed class ObtenerEvaluacionPermanenciaHandler
    : IQueryHandler<ObtenerEvaluacionPermanenciaQuery, EvaluacionPermanenciaDto>
{
    private readonly IReporteAcademicoRepository _reportes;

    public ObtenerEvaluacionPermanenciaHandler(IReporteAcademicoRepository reportes)
    {
        _reportes = reportes ?? throw new ArgumentNullException(nameof(reportes));
    }

    public async Task<EvaluacionPermanenciaDto> HandleAsync(
        ObtenerEvaluacionPermanenciaQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var reporte = await _reportes.ObtenerPorEvaluacionParaConsultaAsync(
            query.EvaluacionPermanenciaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException(
                "evaluación de permanencia", query.EvaluacionPermanenciaId);
        return reporte.Evaluacion is { } evaluacion
            ? EvaluacionPermanenciaDto.Desde(evaluacion)
            : throw new RecursoNoEncontradoException(
                "evaluación de permanencia", query.EvaluacionPermanenciaId);
    }
}
