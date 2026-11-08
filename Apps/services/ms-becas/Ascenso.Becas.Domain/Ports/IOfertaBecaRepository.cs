namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de persistencia de la entidad <c>OfertaBeca</c>.
/// </summary>
public interface IOfertaBecaRepository
{
    Task<bool> ExisteAsync(Guid ofertaId, CancellationToken cancellationToken = default);

    Task<bool> EstaPendienteAsync(Guid ofertaId, CancellationToken cancellationToken = default);
}
