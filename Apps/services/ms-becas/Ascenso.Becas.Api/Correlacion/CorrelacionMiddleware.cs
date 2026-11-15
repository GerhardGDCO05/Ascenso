using Ascenso.Becas.Application.Correlacion;

namespace Ascenso.Becas.Api.Correlacion;

/// <summary>
/// Acepta o genera <c>X-Correlation-Id</c>, lo devuelve en la respuesta y lo
/// publica en el contexto HTTP y en el ámbito de logs.
/// </summary>
public sealed class CorrelacionMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<CorrelacionMiddleware> _logger;

    public CorrelacionMiddleware(
        RequestDelegate siguiente,
        ILogger<CorrelacionMiddleware> logger)
    {
        _siguiente = siguiente ?? throw new ArgumentNullException(nameof(siguiente));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        var correlacion = CorrelacionHttp.ObtenerValido(
                contexto.Request.Headers[CorrelacionConstantes.Encabezado].ToString())
            ?? Guid.NewGuid().ToString("N");

        contexto.TraceIdentifier = correlacion;
        contexto.Items[CorrelacionConstantes.ItemHttp] = correlacion;
        contexto.Response.Headers[CorrelacionConstantes.Encabezado] = correlacion;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlacion
        }))
        {
            await _siguiente(contexto);
        }
    }
}
