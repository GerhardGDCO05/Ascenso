using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Desembolsos.Entidades;
using Ascenso.Becas.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

public sealed class CalendarioDesembolsoRepository : ICalendarioDesembolsoRepository
{
    private readonly BecasDbContext _contexto;

    public CalendarioDesembolsoRepository(BecasDbContext contexto) => _contexto = contexto;

    public async Task AgregarAsync(CalendarioDesembolso calendario, CancellationToken cancellationToken = default)
        => await _contexto.CalendariosDesembolso.AddAsync(calendario, cancellationToken);

    public Task<CalendarioDesembolso?> ObtenerPorIdAsync(
        Guid calendarioId, CancellationToken cancellationToken = default)
        => _contexto.CalendariosDesembolso
            .Include("_desembolsos")
            .Include("_desembolsos._incidencias")
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == calendarioId, cancellationToken);

    public Task<CalendarioDesembolso?> ObtenerPorBecaYPeriodoAsync(
        Guid becaId, Guid periodoId, CancellationToken cancellationToken = default)
        => _contexto.CalendariosDesembolso
            .Include("_desembolsos")
            .Include("_desembolsos._incidencias")
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.BecaId == becaId && item.PeriodoAcademicoId == periodoId,
                cancellationToken);

    public async Task<IReadOnlyList<CalendarioDesembolso>> ObtenerPorBecaAsync(
        Guid becaId, int pagina, int tamanoPagina, CancellationToken cancellationToken = default)
        => await _contexto.CalendariosDesembolso
            .AsNoTracking()
            .Where(item => item.BecaId == becaId)
            .OrderBy(item => item.FechaInicio)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<Desembolso>> ObtenerCuotasAsync(
        Guid calendarioId, int pagina, int tamanoPagina,
        CancellationToken cancellationToken = default)
        => await _contexto.Desembolsos.AsNoTracking()
            .Where(item => item.CalendarioDesembolsoId == calendarioId)
            .OrderBy(item => item.NumeroCuota)
            .ThenBy(item => item.IntentosEjecucion)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToArrayAsync(cancellationToken);
}
