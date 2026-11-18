using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Becas.Api.Errores;

/// <summary>
/// Homogeneiza la respuesta de validación automática de DTOs que produce
/// <c>[ApiController]</c> con el resto de errores de la API.
/// </summary>
public static class ConfiguracionErroresApi
{
    public const string CodigoSolicitudInvalida = "SOLICITUD_INVALIDA";

    private const string MensajeSolicitudInvalida =
        "La solicitud contiene errores de validación.";

    /// <summary>
    /// Sustituye la respuesta de <c>ModelState</c> inválido por un
    /// <c>ProblemDetails</c> con <c>code</c>, <c>correlationId</c> y
    /// <c>errors</c>.
    /// </summary>
    public static IMvcBuilder AgregarProblemDetailsDeValidacion(
        this IMvcBuilder constructor)
    {
        ArgumentNullException.ThrowIfNull(constructor);

        constructor.ConfigureApiBehaviorOptions(opciones =>
        {
            opciones.InvalidModelStateResponseFactory = CrearRespuestaValidacion;
        });

        return constructor;
    }

    private static ObjectResult CrearRespuestaValidacion(ActionContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        var errores = contexto.ModelState
            .Where(entrada => entrada.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entrada => string.IsNullOrWhiteSpace(entrada.Key)
                    ? "solicitud"
                    : entrada.Key,
                entrada => entrada.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "La solicitud no es válida."
                        : error.ErrorMessage)
                    .ToArray(),
                StringComparer.Ordinal);

        var problema = EscritorProblemDetails.Crear(
            contexto.HttpContext,
            StatusCodes.Status400BadRequest,
            CodigoSolicitudInvalida,
            MensajeSolicitudInvalida,
            errores);

        return new ObjectResult(problema)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
    }
}
