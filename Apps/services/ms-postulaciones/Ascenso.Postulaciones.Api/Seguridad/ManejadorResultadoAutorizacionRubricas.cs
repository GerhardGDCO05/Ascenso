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
            var esPoliticaComites = EndpointTienePoliticaDeComites(context);
            var esPoliticaAsignaciones = EndpointTienePoliticaDeAsignaciones(context);
            var esPoliticaEvaluaciones = EndpointTienePoliticaDeEvaluaciones(context);
            var esPoliticaConsolidacion = EndpointTienePoliticaDeConsolidacion(context);
            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Acceso denegado",
                Detail = esPoliticaConsolidacion
                    ? "El usuario autenticado no tiene permisos para ejecutar esta operación sobre los resultados consolidados y alertas de discrepancia."
                    : esPoliticaEvaluaciones
                        ? "El usuario autenticado no tiene permisos para ejecutar esta operación sobre las evaluaciones de postulaciones."
                        : esPoliticaAsignaciones
                            ? "El usuario autenticado no tiene permisos para ejecutar esta operación sobre las asignaciones de evaluación."
                            : esPoliticaComites
                                ? "El usuario autenticado no tiene permisos para ejecutar esta operación sobre los comités evaluadores."
                                : "El usuario autenticado no tiene permisos para ejecutar esta operación sobre las rúbricas de evaluación.",
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
            || string.Equals(dato.Policy, PoliticasAutorizacionRubricas.LecturaRubrica, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionComites.GestionComitesEvaluadores, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionAsignaciones.GestionAsignacionesEvaluacion, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionAsignaciones.ConsultaMisAsignaciones, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionEvaluaciones.SupervisionEvaluaciones, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionConsolidacion.ConsultaConsolidacion, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionConsolidacion.GestionConsolidacion, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionConsolidacion.ConsultaAdjudicabilidadInterna, StringComparison.Ordinal));
    }

    private static bool EndpointTienePoliticaDeComites(HttpContext context)
    {
        var metadatosAutorizacion = context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>();
        if (metadatosAutorizacion is null || metadatosAutorizacion.Count == 0)
        {
            return false;
        }

        return metadatosAutorizacion.Any(dato =>
            string.Equals(dato.Policy, PoliticasAutorizacionComites.GestionComitesEvaluadores, StringComparison.Ordinal));
    }

    private static bool EndpointTienePoliticaDeAsignaciones(HttpContext context)
    {
        var metadatosAutorizacion = context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>();
        if (metadatosAutorizacion is null || metadatosAutorizacion.Count == 0)
        {
            return false;
        }

        return metadatosAutorizacion.Any(dato =>
            string.Equals(dato.Policy, PoliticasAutorizacionAsignaciones.GestionAsignacionesEvaluacion, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionAsignaciones.ConsultaMisAsignaciones, StringComparison.Ordinal));
    }

    private static bool EndpointTienePoliticaDeEvaluaciones(HttpContext context)
    {
        var metadatosAutorizacion = context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>();
        if (metadatosAutorizacion is null || metadatosAutorizacion.Count == 0)
        {
            return false;
        }

        return metadatosAutorizacion.Any(dato =>
            string.Equals(dato.Policy, PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionEvaluaciones.SupervisionEvaluaciones, StringComparison.Ordinal));
    }

    private static bool EndpointTienePoliticaDeConsolidacion(HttpContext context)
    {
        var metadatosAutorizacion = context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>();
        if (metadatosAutorizacion is null || metadatosAutorizacion.Count == 0)
        {
            return false;
        }

        return metadatosAutorizacion.Any(dato =>
            string.Equals(dato.Policy, PoliticasAutorizacionConsolidacion.ConsultaConsolidacion, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionConsolidacion.GestionConsolidacion, StringComparison.Ordinal)
            || string.Equals(dato.Policy, PoliticasAutorizacionConsolidacion.ConsultaAdjudicabilidadInterna, StringComparison.Ordinal));
    }
}
