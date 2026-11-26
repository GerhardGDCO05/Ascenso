using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.RegistrarReporteAcademico;

public sealed record RegistrarReporteAcademicoCommand(
    Guid BecaId, Guid PeriodoAcademicoId, decimal PromedioPeriodo,
    int MateriasAprobadas, int MateriasReprobadas, int MateriasCursando,
    int CreditosAprobados, EstadoMatricula EstadoMatricula,
    string UrlComprobanteNotasFirebase, string HashComprobanteSha256,
    long TamanioComprobanteBytes, string TipoMimeComprobante,
    FuenteReporte FuenteEntrega, Guid UniversidadAliadaId,
    int? HorasServicioCumplidas = null,
    string? UrlComprobanteServicio = null,
    string? HashComprobanteServicioSha256 = null)
    : ICommand<ReporteAcademicoDto>;
