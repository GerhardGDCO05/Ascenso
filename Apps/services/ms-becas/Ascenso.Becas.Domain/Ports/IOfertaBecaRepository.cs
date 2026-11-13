using Ascenso.Becas.Domain.Otorgamiento.Entidades;

namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de persistencia de la entidad <c>OfertaBeca</c>.
/// </summary>
public interface IOfertaBecaRepository
{
    /// <summary>
    /// Recupera la oferta con su snapshot de condiciones de permanencia.
    /// </summary>
    Task<OfertaBeca?> ObtenerPorIdAsync(Guid ofertaId, CancellationToken cancellationToken = default);

    Task AgregarAsync(OfertaBeca oferta, CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(Guid ofertaId, CancellationToken cancellationToken = default);

    Task<bool> EstaPendienteAsync(Guid ofertaId, CancellationToken cancellationToken = default);
}
