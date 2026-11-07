namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de persistencia de la entidad <c>CompromisoBecario</c>.
/// </summary>
public interface ICompromisoBecarioRepository
{
    Task<bool> ExisteAsync(Guid compromisoId, CancellationToken cancellationToken = default);

    Task<bool> ExisteAprobadoParaBecaAsync(Guid becaId, CancellationToken cancellationToken = default);
}
