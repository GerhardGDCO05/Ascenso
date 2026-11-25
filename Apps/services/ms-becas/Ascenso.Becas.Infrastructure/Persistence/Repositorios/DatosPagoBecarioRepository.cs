using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

public sealed class DatosPagoBecarioRepository : IDatosPagoBecarioRepository
{
    private readonly BecasDbContext _contexto;

    public DatosPagoBecarioRepository(BecasDbContext contexto) => _contexto = contexto;

    public async Task AgregarAsync(DatosPagoBecario datos, CancellationToken cancellationToken = default)
        => await _contexto.DatosPagoBecarios.AddAsync(datos, cancellationToken);

    public Task<DatosPagoBecario?> ObtenerActivoPorBecaAsync(
        Guid becaId, CancellationToken cancellationToken = default)
        => _contexto.DatosPagoBecarios.SingleOrDefaultAsync(
            item => item.BecaId == becaId && item.Estado == EstadoDatosPago.Activo,
            cancellationToken);

    public async Task<IReadOnlyList<DatosPagoBecario>> ObtenerHistorialPorBecaAsync(
        Guid becaId, CancellationToken cancellationToken = default)
        => await _contexto.DatosPagoBecarios.AsNoTracking()
            .Where(item => item.BecaId == becaId)
            .OrderByDescending(item => item.FechaCreacion)
            .ToArrayAsync(cancellationToken);
}
