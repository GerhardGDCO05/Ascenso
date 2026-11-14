using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Exige evidencia de segundo factor a los usuarios con un rol administrativo.
/// Acepta el valor configurado de <c>acr</c> o alguno de los valores de
/// <c>amr</c> configurados (<c>otp</c>/<c>totp</c> por defecto).
/// </summary>
public sealed class Requiere2FAHandler : AuthorizationHandler<Requiere2FARequirement>
{
    private readonly AutorizacionOptions _opciones;

    public Requiere2FAHandler(IOptions<AutorizacionOptions> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _opciones = opciones.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        Requiere2FARequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        // Los roles no administrativos no requieren segundo factor en este
        // servicio. El requisito se satisface para que un rechazo por rol se
        // resuelva como 403 y no como 401 de segundo factor.
        if (!TieneRolAdministrativo(context.User))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (TieneSegundoFactor(context.User))
        {
            context.Succeed(requirement);
        }

        // Sin evidencia de segundo factor el requisito queda sin satisfacer;
        // el evaluador lo incluye en AuthorizationFailure.FailedRequirements
        // para que el manejador de resultados responda 401.
        return Task.CompletedTask;
    }

    private bool TieneRolAdministrativo(ClaimsPrincipal usuario)
    {
        var roles = LectorClaims.Valores(usuario, _opciones.ClaimRol);

        return _opciones.RolesAdministrativos.Any(administrativo =>
            roles.Contains(administrativo, StringComparer.OrdinalIgnoreCase));
    }

    private bool TieneSegundoFactor(ClaimsPrincipal usuario)
    {
        if (!string.IsNullOrWhiteSpace(_opciones.AcrSegundoFactor)
            && LectorClaims.Valores(usuario, _opciones.ClaimAcr).Any(valor =>
                string.Equals(
                    valor,
                    _opciones.AcrSegundoFactor,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var amr = LectorClaims.Valores(usuario, _opciones.ClaimAmr);

        return _opciones.AmrSegundoFactor.Any(aceptado =>
            !string.IsNullOrWhiteSpace(aceptado)
            && amr.Contains(aceptado, StringComparer.OrdinalIgnoreCase));
    }
}
