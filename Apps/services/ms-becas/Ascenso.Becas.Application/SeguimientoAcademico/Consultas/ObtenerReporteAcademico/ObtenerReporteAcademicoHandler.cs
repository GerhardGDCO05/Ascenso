using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Consultas.ObtenerReporteAcademico;

public sealed class ObtenerReporteAcademicoHandler
    : IQueryHandler<ObtenerReporteAcademicoQuery, ReporteAcademicoDto>
{
    private readonly IReporteAcademicoRepository _reportes;

    public ObtenerReporteAcademicoHandler(IReporteAcademicoRepository reportes)
    {
        _reportes = reportes ?? throw new ArgumentNullException(nameof(reportes));
    }

    public async Task<ReporteAcademicoDto> HandleAsync(
        ObtenerReporteAcademicoQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var reporte = await _reportes.ObtenerParaConsultaAsync(
            query.ReporteAcademicoId, cancellationToken)
            ?? throw new RecursoNoEncontradoException(
                "reporte académico", query.ReporteAcademicoId);
        return ReporteAcademicoDto.Desde(reporte);
    }
}
