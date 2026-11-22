using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Seguridad;

public sealed class ManejadorResultadoAutorizacionRubricas : IAuthorizationMiddlewareResultHandler
{
    public const string CodigoPermisoDenegado = "PERMISO_DENEGADO";

    private readonly AuthorizationMiddlewareResultHandler _manejadorPredeterminado = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        if (authorizeResult.Forbidden && EndpointTienePoliticaDeRubricas(context))
        {
            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Acceso denegado",
                Detail = "El usuario autenticado no tiene permisos para ejecutar esta operación sobre las rúbricas de evaluación.",
                Instance = context.Request.Path
            };
            problema.Extensions["codigo"] = CodigoPermisoDenegado;

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(problema, context.RequestAborted);
            return;
        }

        await _manejadorPredeterminado.HandleAsync(next, context, policy, authorizeResult);
    }

    private static bool EndpointTienePoliticaDeRubricas(HttpContext context)
    {
        var metadatosAutorizacion = context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>();
        if (metadatosAutorizacion is null || metadatosAutorizacion.Count == 0)
        {
            return false;
        }

        return metadatosAutorizacion.Any(dato =>
            string.Equals(dato.Policy, PoliticasAutorizacionRubricas.CoordinadorPrograma, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionRubricas.LecturaRubrica, StringComparison.Ordinal));
    }
}
