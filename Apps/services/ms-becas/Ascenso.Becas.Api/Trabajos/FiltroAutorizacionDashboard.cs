using Ascenso.Becas.Api.Seguridad;
using Hangfire.Dashboard;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Api.Trabajos;

/// <summary>
/// Autoriza el dashboard de Hangfire solo a usuarios administrativos con
/// segundo factor. Un usuario no autenticado recibe 401 y uno autenticado sin
/// permiso recibe 403, según el comportamiento del middleware de Hangfire.
/// </summary>
public sealed class FiltroAutorizacionDashboard : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var http = context.GetHttpContext();

        if (http.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var opciones = http.RequestServices
            .GetRequiredService<IOptions<AutorizacionOptions>>()
            .Value;

        var roles = LectorClaims.Valores(http.User, opciones.ClaimRol);

        if (!opciones.RolesAdministrativos.Any(rol =>
                roles.Contains(rol, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(opciones.AcrSegundoFactor)
            && LectorClaims.Valores(http.User, opciones.ClaimAcr).Any(valor =>
                string.Equals(
                    valor,
                    opciones.AcrSegundoFactor,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var amr = LectorClaims.Valores(http.User, opciones.ClaimAmr);

        return opciones.AmrSegundoFactor.Any(aceptado =>
            !string.IsNullOrWhiteSpace(aceptado)
            && amr.Contains(aceptado, StringComparer.OrdinalIgnoreCase));
    }
}
