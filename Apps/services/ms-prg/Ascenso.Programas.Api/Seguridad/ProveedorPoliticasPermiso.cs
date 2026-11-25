using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Ascenso.Programas.Api.Seguridad;

/// <summary>
/// Proveedor dinámico de políticas: resuelve primero las políticas nombradas y,
/// si no existe, construye una política PBAC para los permisos <c>convocatorias:*</c>.
/// </summary>
public sealed class ProveedorPoliticasPermiso : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _proveedorPredeterminado;

    public ProveedorPoliticasPermiso(IOptions<AuthorizationOptions> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _proveedorPredeterminado = new DefaultAuthorizationPolicyProvider(opciones);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!PermisosConvocatoria.EsPoliticaConvocatoria(policyName))
        {
            return _proveedorPredeterminado.GetPolicyAsync(policyName);
        }

        var politica = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new RequisitoPermiso(policyName))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(politica);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _proveedorPredeterminado.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _proveedorPredeterminado.GetFallbackPolicyAsync();
}
