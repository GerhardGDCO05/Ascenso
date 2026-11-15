using System.Security.Claims;
using Ascenso.Postulaciones.Application.Interfaces;

namespace Ascenso.Postulaciones.Api.Seguridad;

/// <summary>
/// Adaptador que resuelve la identidad del usuario autenticado desde el JWT de Keycloak.
/// </summary>
public sealed class UsuarioActualJwt : IUsuarioActual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActualJwt(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public bool EstaAutenticado
        => (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false)
            && UsuarioId != Guid.Empty;

    public Guid UsuarioId
    {
        get
        {
            var usuario = _httpContextAccessor.HttpContext?.User;
            if (usuario?.Identity?.IsAuthenticated != true)
            {
                return Guid.Empty;
            }

            var sub = usuario.FindFirst("sub")?.Value
                ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(sub, out var usuarioId) && usuarioId != Guid.Empty
                ? usuarioId
                : Guid.Empty;
        }
    }
}
