using Ascenso.Becas.Domain.Otorgamiento.Entidades;

namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de persistencia de la entidad <c>CompromisoBecario</c>.
/// </summary>
public interface ICompromisoBecarioRepository
{
    Task<CompromisoBecario?> ObtenerPorIdAsync(
        Guid compromisoId,
        CancellationToken cancellationToken = default);

    Task<CompromisoBecario?> ObtenerPorBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(CompromisoBecario compromiso, CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(Guid compromisoId, CancellationToken cancellationToken = default);

    Task<bool> ExisteAprobadoParaBecaAsync(Guid becaId, CancellationToken cancellationToken = default);
}
