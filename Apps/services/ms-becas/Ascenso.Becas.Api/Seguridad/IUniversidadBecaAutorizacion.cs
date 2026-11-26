using System.Security.Claims;

namespace Ascenso.Becas.Api.Seguridad;

/// <summary>Comprueba la relación de una universidad con una beca concreta.</summary>
public interface IUniversidadBecaAutorizacion
{
    Task<bool> PuedeEntregarAsync(ClaimsPrincipal usuario, Guid becaId,
        Guid universidadId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Falla cerrado mientras no exista un contrato que verifique la relación
/// universidad-beca en el servicio propietario.
/// </summary>
public sealed class UniversidadBecaAutorizacionNoDisponible : IUniversidadBecaAutorizacion
{
    public Task<bool> PuedeEntregarAsync(ClaimsPrincipal usuario, Guid becaId,
        Guid universidadId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
