using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Ascenso.Becas.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del puerto de la entidad <c>OfertaBeca</c>. El
/// snapshot de condiciones se carga automáticamente por ser un tipo propiedad.
/// </summary>
public sealed class OfertaBecaRepository : IOfertaBecaRepository
{
    private readonly BecasDbContext _contexto;

    public OfertaBecaRepository(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<OfertaBeca?> ObtenerPorIdAsync(
        Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        if (ofertaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la oferta es obligatorio.", nameof(ofertaId));
        }

        return _contexto.OfertasBeca
            .SingleOrDefaultAsync(oferta => oferta.Id == ofertaId, cancellationToken);
    }

    public async Task AgregarAsync(OfertaBeca oferta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(oferta);
        await _contexto.OfertasBeca.AddAsync(oferta, cancellationToken);
    }

    public Task<bool> ExisteAsync(Guid ofertaId, CancellationToken cancellationToken = default)
        => _contexto.OfertasBeca
            .AsNoTracking()
            .AnyAsync(oferta => oferta.Id == ofertaId, cancellationToken);

    public Task<bool> EstaPendienteAsync(
        Guid ofertaId,
        CancellationToken cancellationToken = default)
        => _contexto.OfertasBeca
            .AsNoTracking()
            .AnyAsync(
                oferta => oferta.Id == ofertaId
                    && oferta.Estado == EstadoOferta.PendienteRespuesta,
                cancellationToken);
}
