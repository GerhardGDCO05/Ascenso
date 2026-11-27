using System.Security.Claims;
using Ascenso.Postulaciones.Application.Interfaces;

namespace Ascenso.Postulaciones.Api.Seguridad;

/// <summary>
/// Adaptador que consulta los roles asignados al usuario autenticado actual desde el principal JWT.
/// </summary>
public sealed class RolesUsuarioActualJwt : IRolesUsuarioActual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RolesUsuarioActualJwt(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public bool TieneRol(string rol)
    {
        if (string.IsNullOrWhiteSpace(rol))
        {
            throw new ArgumentException(
                "El nombre del rol a verificar es obligatorio.",
                nameof(rol));
        }

        var usuario = _httpContextAccessor.HttpContext?.User;
        if (usuario?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var rolNormalizado = rol.Trim();

        return usuario.IsInRole(rolNormalizado)
            || usuario.Claims.Any(claim =>
                (string.Equals(claim.Type, "roles", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
                && string.Equals(claim.Value, rolNormalizado, StringComparison.Ordinal));
    }
}
