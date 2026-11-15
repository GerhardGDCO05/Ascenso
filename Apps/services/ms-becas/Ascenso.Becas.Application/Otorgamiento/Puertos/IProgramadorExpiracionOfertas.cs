namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Programa la expiración de una oferta en el planificador de trabajos. El
/// adaptador concreto vive en infraestructura; la capa de aplicación solo
/// conoce el contrato.
/// </summary>
public interface IProgramadorExpiracionOfertas
{
    Task<string> ProgramarAsync(
        Guid ofertaId,
        DateTimeOffset fechaVencimiento,
        CancellationToken cancellationToken = default);

    Task CancelarAsync(
        string referenciaJob,
        CancellationToken cancellationToken = default);
}
