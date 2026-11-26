using Ascenso.Becas.Domain.Desembolsos.Agregados;

namespace Ascenso.Becas.Domain.Ports;

public interface IDatosPagoBecarioRepository
{
    Task AgregarAsync(DatosPagoBecario datos, CancellationToken cancellationToken = default);

    Task<DatosPagoBecario?> ObtenerActivoPorBecaAsync(
        Guid becaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatosPagoBecario>> ObtenerHistorialPorBecaAsync(
        Guid becaId, CancellationToken cancellationToken = default);
}
