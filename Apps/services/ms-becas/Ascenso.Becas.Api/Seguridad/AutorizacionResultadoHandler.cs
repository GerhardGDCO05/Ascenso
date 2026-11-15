using Ascenso.Becas.Api.Errores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Convierte fallos de autorización en <c>ProblemDetails</c>. Un fallo de 2FA
/// responde 401 con <c>SEGUNDO_FACTOR_REQUERIDO</c>; el resto responde 403 con
/// <c>PERMISO_DENEGADO</c>.
/// </summary>
public sealed class AutorizacionResultadoHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        if (authorizeResult.Challenged)
        {
            await EscritorProblemDetails.EscribirAsync(
                context,
                401,
                "NO_AUTENTICADO",
                "Se requiere un token de acceso válido.");
            return;
        }

        var requisitoFallido = authorizeResult.AuthorizationFailure?
            .FailedRequirements
            .FirstOrDefault();

        if (requisitoFallido is Requiere2FARequirement)
        {
            await EscritorProblemDetails.EscribirAsync(
                context,
                401,
                "SEGUNDO_FACTOR_REQUERIDO",
                "La operación exige completar el segundo factor de autenticación.");
            return;
        }

        await EscritorProblemDetails.EscribirAsync(
            context,
            403,
            "PERMISO_DENEGADO",
            "El usuario no tiene permisos suficientes para realizar la operación.");
    }
}
