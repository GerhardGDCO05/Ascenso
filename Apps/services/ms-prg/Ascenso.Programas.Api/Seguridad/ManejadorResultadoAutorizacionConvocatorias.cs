using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Programas.Api.Seguridad;

/// <summary>
/// Devuelve <c>403</c> con <c>codigo = PERMISO_DENEGADO</c> cuando la autorización
/// falla en una política de convocatorias; el resto de resultados se delega.
/// </summary>
public sealed class ManejadorResultadoAutorizacionConvocatorias : IAuthorizationMiddlewareResultHandler
{
    public const string CodigoPermisoDenegado = "PERMISO_DENEGADO";

    private readonly AuthorizationMiddlewareResultHandler _manejadorPredeterminado = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);

        if (authorizeResult.Forbidden && EsPoliticaDeConvocatorias(context, policy))
        {
            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Permiso denegado",
                Detail = "El token no incluye el permiso requerido para ejecutar esta operación.",
                Instance = context.Request.Path
            };
            problema.Extensions["codigo"] = CodigoPermisoDenegado;

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(problema);
            return;
        }

        await _manejadorPredeterminado.HandleAsync(next, context, policy, authorizeResult);
    }

    private static bool EsPoliticaDeConvocatorias(
        HttpContext context,
        AuthorizationPolicy policy)
    {
        if (policy.Requirements.OfType<RequisitoPermiso>().Any(
            requisito => PermisosConvocatoria.EsPoliticaConvocatoria(requisito.Permiso)))
        {
            return true;
        }

        return context.GetEndpoint()?.Metadata
            .GetOrderedMetadata<AuthorizeAttribute>()
            .Any(atributo => PermisosConvocatoria.EsPoliticaConvocatoria(atributo.Policy)) == true;
    }
}
