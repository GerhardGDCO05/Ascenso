using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Dtos;

public sealed record ReporteAcademicoDto(
    Guid Id, Guid BecaId, Guid PeriodoAcademicoId,
    decimal PromedioPeriodo, int MateriasAprobadas,
    int MateriasReprobadas, int MateriasCursando,
    int CreditosAprobados, int? HorasServicioCumplidas,
    EstadoMatricula EstadoMatricula, string UrlComprobanteNotasFirebase,
    string HashComprobanteSha256, long TamanioComprobanteBytes,
    string TipoMimeComprobante, FuenteReporte FuenteEntrega,
    Guid UniversidadAliadaId, DateTimeOffset FechaEntrega,
    DateOnly FechaLimitePeriodo, bool EntregaTardia,
    EstadoReporteAcademico Estado, Guid? RevisadoPorUsuarioId,
    DateTimeOffset? FechaRevision, EvaluacionPermanenciaDto? Evaluacion)
{
    public static ReporteAcademicoDto Desde(ReporteAcademico reporte)
        => new(reporte.Id, reporte.BecaId, reporte.PeriodoAcademicoId,
            reporte.PromedioPeriodo, reporte.MateriasAprobadas,
            reporte.MateriasReprobadas, reporte.MateriasCursando,
            reporte.CreditosAprobados, reporte.HorasServicioCumplidas,
            reporte.EstadoMatricula, reporte.UrlComprobanteNotasFirebase,
            reporte.HashComprobanteSha256, reporte.TamanioComprobanteBytes,
            reporte.TipoMimeComprobante, reporte.FuenteEntrega,
            reporte.UniversidadAliadaId, reporte.FechaEntrega,
            reporte.FechaLimitePeriodo, reporte.EntregaTardia, reporte.Estado,
            reporte.RevisadoPorUsuarioId, reporte.FechaRevision,
            reporte.Evaluacion is null ? null : EvaluacionPermanenciaDto.Desde(reporte.Evaluacion));
}
