using Ascenso.Programas.Domain.Excepciones;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Programas.Api.Middlewares;

/// <summary>
/// Traduce excepciones de validación y dominio en respuestas ProblemDetails estandarizadas.
/// </summary>
public sealed class ManejadorExcepcionesDominio : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is DomainException domainException)
        {
            var problemaDominio = new ProblemDetails
            {
                Status = domainException.EstadoHttp,
                Title = "Error de regla de negocio",
                Detail = domainException.Message,
                Instance = httpContext.Request.Path
            };
            problemaDominio.Extensions["codigo"] = domainException.Codigo;

            httpContext.Response.StatusCode = domainException.EstadoHttp;
            await httpContext.Response.WriteAsJsonAsync(problemaDominio, cancellationToken);
            return true;
        }

        if (exception is ValidationException validationException)
        {
            var primerCodigo = validationException.Errors
                .Select(error => error.ErrorCode)
                .FirstOrDefault(codigo => !string.IsNullOrWhiteSpace(codigo))
                ?? "VALIDACION_INVALIDA";

            var erroresPorCampo = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Select(error => error.ErrorMessage).ToArray());

            var problemaValidacion = new ValidationProblemDetails(erroresPorCampo)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de validación de solicitud",
                Detail = "Uno o más campos de la solicitud no cumplen con las reglas requeridas.",
                Instance = httpContext.Request.Path
            };
            problemaValidacion.Extensions["codigo"] = primerCodigo;

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(problemaValidacion, cancellationToken);
            return true;
        }

        if (exception is ArgumentException argumentException)
        {
            var problemaArgumento = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Solicitud inválida",
                Detail = argumentException.Message,
                Instance = httpContext.Request.Path
            };
            problemaArgumento.Extensions["codigo"] = "SOLICITUD_INVALIDA";

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(problemaArgumento, cancellationToken);
            return true;
        }

        return false;
    }
}

