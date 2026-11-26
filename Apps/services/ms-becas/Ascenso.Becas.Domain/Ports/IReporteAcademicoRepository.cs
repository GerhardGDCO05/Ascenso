using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;

namespace Ascenso.Becas.Domain.Ports;

/// <summary>Persistencia de la raíz de seguimiento académico.</summary>
public interface IReporteAcademicoRepository
{
    Task<ReporteAcademico?> ObtenerPorIdAsync(
        Guid reporteId, CancellationToken cancellationToken = default);

    Task<ReporteAcademico?> ObtenerParaConsultaAsync(
        Guid reporteId, CancellationToken cancellationToken = default);

    Task<ReporteAcademico?> ObtenerPorEvaluacionParaConsultaAsync(
        Guid evaluacionId, CancellationToken cancellationToken = default);

    Task<ReporteAcademico?> ObtenerPorBecaYPeriodoParaConsultaAsync(
        Guid becaId, Guid periodoAcademicoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistePorBecaYPeriodoAsync(
        Guid becaId, Guid periodoAcademicoId,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        ReporteAcademico reporte, CancellationToken cancellationToken = default);

    /// <summary>Registra como nueva la evaluación creada sobre un reporte persistido.</summary>
    void PrepararNuevaEvaluacion(ReporteAcademico reporte);
}
