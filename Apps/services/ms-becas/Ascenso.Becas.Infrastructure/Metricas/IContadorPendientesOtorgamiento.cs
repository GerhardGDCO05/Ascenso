namespace Ascenso.Becas.Infrastructure.Metricas;

/// <summary>
/// Obtiene los pendientes que se publican como gauges de negocio.
/// </summary>
public interface IContadorPendientesOtorgamiento
{
    Task<ResultadoPendientesOtorgamiento> ContarAsync(
        CancellationToken cancellationToken = default);
}
