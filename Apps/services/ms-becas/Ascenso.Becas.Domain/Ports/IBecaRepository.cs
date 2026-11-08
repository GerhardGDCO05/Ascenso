namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de persistencia del agregado <c>Beca</c>. Las operaciones de
/// escritura se completarán cuando el agregado se implemente en la Feature 3.
/// </summary>
public interface IBecaRepository
{
    Task<bool> ExisteAsync(Guid becaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteActivaConCoberturaTotalAsync(
        Guid estudianteId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteActivaEnPeriodoAsync(
        Guid estudianteId,
        Guid periodoAcademicoId,
        CancellationToken cancellationToken = default);
}
