using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Autoriza únicamente a los clientes de servicio permitidos que presentan el
/// alcance interno de becas. Sin cliente o sin alcance se falla cerrado.
/// </summary>
public sealed class CuentaServicioHandler : AuthorizationHandler<CuentaServicioRequirement>
{
    private readonly AutorizacionOptions _opciones;

    public CuentaServicioHandler(IOptions<AutorizacionOptions> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _opciones = opciones.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CuentaServicioRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var cliente = LectorClaims.PrimerValor(
            context.User,
            _opciones.ClaimClientId);

        if (string.IsNullOrWhiteSpace(cliente)
            || !_opciones.ClientesServicioPermitidos.Any(permitido =>
                string.Equals(
                    permitido,
                    cliente,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return Task.CompletedTask;
        }

        var alcances = LectorClaims.Valores(context.User, _opciones.ClaimScope);

        if (alcances.Contains(_opciones.ScopeInterno, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
