using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

public sealed class ReporteAcademicoRepository : IReporteAcademicoRepository
{
    private readonly BecasDbContext _contexto;

    public ReporteAcademicoRepository(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<ReporteAcademico?> ObtenerPorIdAsync(
        Guid reporteId, CancellationToken cancellationToken = default)
        => _contexto.ReportesAcademicos
            .Include(reporte => reporte.Evaluacion!)
            .ThenInclude(evaluacion => evaluacion.DetalleCondiciones)
            .AsSplitQuery()
            .SingleOrDefaultAsync(reporte => reporte.Id == reporteId, cancellationToken);

    public Task<ReporteAcademico?> ObtenerParaConsultaAsync(
        Guid reporteId, CancellationToken cancellationToken = default)
        => _contexto.ReportesAcademicos.AsNoTracking()
            .Include(reporte => reporte.Evaluacion!)
            .ThenInclude(evaluacion => evaluacion.DetalleCondiciones)
            .AsSplitQuery()
            .SingleOrDefaultAsync(reporte => reporte.Id == reporteId, cancellationToken);

    public Task<ReporteAcademico?> ObtenerPorEvaluacionParaConsultaAsync(
        Guid evaluacionId, CancellationToken cancellationToken = default)
        => _contexto.ReportesAcademicos.AsNoTracking()
            .Include(reporte => reporte.Evaluacion!)
            .ThenInclude(evaluacion => evaluacion.DetalleCondiciones)
            .AsSplitQuery()
            .SingleOrDefaultAsync(reporte => reporte.Evaluacion != null
                && reporte.Evaluacion.Id == evaluacionId, cancellationToken);

    public Task<ReporteAcademico?> ObtenerPorBecaYPeriodoParaConsultaAsync(
        Guid becaId, Guid periodoAcademicoId,
        CancellationToken cancellationToken = default)
        => _contexto.ReportesAcademicos.AsNoTracking()
            .Include(reporte => reporte.Evaluacion!)
            .ThenInclude(evaluacion => evaluacion.DetalleCondiciones)
            .AsSplitQuery()
            .SingleOrDefaultAsync(reporte => reporte.BecaId == becaId
                && reporte.PeriodoAcademicoId == periodoAcademicoId,
                cancellationToken);

    public Task<bool> ExistePorBecaYPeriodoAsync(
        Guid becaId, Guid periodoAcademicoId,
        CancellationToken cancellationToken = default)
        => _contexto.ReportesAcademicos.AnyAsync(
            reporte => reporte.BecaId == becaId
                && reporte.PeriodoAcademicoId == periodoAcademicoId,
            cancellationToken);

    public async Task AgregarAsync(
        ReporteAcademico reporte, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reporte);
        await _contexto.ReportesAcademicos.AddAsync(reporte, cancellationToken);
    }

    public void PrepararNuevaEvaluacion(ReporteAcademico reporte)
    {
        ArgumentNullException.ThrowIfNull(reporte);
        if (reporte.Evaluacion is null)
            throw new InvalidOperationException("El reporte no tiene una evaluación nueva.");
        _contexto.Entry(reporte.Evaluacion).State = EntityState.Added;
        foreach (CondicionPermanenciaEvaluada detalle in reporte.Evaluacion.DetalleCondiciones)
            _contexto.Entry(detalle).State = EntityState.Added;
    }
}
