using System.Text.Json;
using Ascenso.Becas.Api.Correlacion;

namespace Ascenso.Becas.Api.Errores;

/// <summary>
/// Escribe respuestas <c>application/problem+json</c> homogéneas para
/// autenticación, autorización y excepciones.
/// </summary>
public static class EscritorProblemDetails
{
    private static readonly JsonSerializerOptions _opciones =
        new(JsonSerializerDefaults.Web);

    public static Task EscribirAsync(
        HttpContext contexto,
        int estadoHttp,
        string codigo,
        string mensaje,
        IDictionary<string, string[]>? errores = null)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(mensaje);

        if (contexto.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        contexto.Response.StatusCode = estadoHttp;
        contexto.Response.ContentType = "application/problem+json";

        var problema = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = estadoHttp,
            Title = Titulo(estadoHttp),
            Detail = mensaje,
            Instance = contexto.Request.Path.Value
        };

        problema.Extensions["code"] = codigo;
        problema.Extensions["correlationId"] = CorrelacionHttp.Obtener(contexto);

        if (errores is { Count: > 0 })
        {
            problema.Extensions["errors"] = errores;
        }

        return contexto.Response.WriteAsync(
            JsonSerializer.Serialize(problema, _opciones),
            contexto.RequestAborted);
    }

    private static string Titulo(int estadoHttp) => estadoHttp switch
    {
        StatusCodes.Status400BadRequest => "Solicitud inválida",
        StatusCodes.Status401Unauthorized => "No autenticado",
        StatusCodes.Status403Forbidden => "Acceso denegado",
        StatusCodes.Status404NotFound => "Recurso no encontrado",
        StatusCodes.Status409Conflict => "Conflicto",
        StatusCodes.Status412PreconditionFailed => "Precondición fallida",
        StatusCodes.Status422UnprocessableEntity => "Entidad no procesable",
        499 => "Operación cancelada",
        _ => "Error interno"
    };
}
