using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.SeguimientoAcademico.Datos;

public sealed record DatosReporteAcademico(
    Guid BecaId,
    Guid PeriodoAcademicoId,
    decimal PromedioPeriodo,
    int MateriasAprobadas,
    int MateriasReprobadas,
    int MateriasCursando,
    int CreditosAprobados,
    EstadoMatricula EstadoMatricula,
    string UrlComprobanteNotasFirebase,
    string HashComprobanteSha256,
    long TamanioComprobanteBytes,
    string TipoMimeComprobante,
    FuenteReporte FuenteEntrega,
    Guid UniversidadAliadaId,
    DateOnly FechaLimitePeriodo);
