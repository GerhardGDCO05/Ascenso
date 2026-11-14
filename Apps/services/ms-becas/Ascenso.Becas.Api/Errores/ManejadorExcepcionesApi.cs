using Ascenso.Becas.Api.Correlacion;
using Microsoft.AspNetCore.Diagnostics;

namespace Ascenso.Becas.Api.Errores;

/// <summary>
/// Middleware de excepciones que devuelve <c>ProblemDetails</c> sin filtrar
/// stack traces ni detalles internos en producción.
/// </summary>
public sealed partial class ManejadorExcepcionesApi : IExceptionHandler
{
    private readonly ILogger<ManejadorExcepcionesApi> _logger;

    public ManejadorExcepcionesApi(ILogger<ManejadorExcepcionesApi> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var resultado = MapeadorExcepcionesDominio.Mapear(exception);
        var correlacionId = CorrelacionHttp.Obtener(httpContext);

        if (resultado.EstadoHttp >= 500)
        {
            RegistrarErrorNoControlado(_logger, exception, correlacionId, resultado.Codigo);
        }
        else
        {
            RegistrarSolicitudRechazada(
                _logger,
                correlacionId,
                resultado.Codigo,
                resultado.EstadoHttp);
        }

        await EscritorProblemDetails.EscribirAsync(
            httpContext,
            resultado.EstadoHttp,
            resultado.Codigo,
            resultado.Mensaje);

        return true;
    }

    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Error,
        Message = "Error no controlado. CorrelationId={correlacionId} Codigo={codigo}")]
    private static partial void RegistrarErrorNoControlado(
        ILogger logger,
        Exception exception,
        string correlacionId,
        string codigo);

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Warning,
        Message = "Solicitud rechazada. CorrelationId={correlacionId} Codigo={codigo} Estado={estado}")]
    private static partial void RegistrarSolicitudRechazada(
        ILogger logger,
        string correlacionId,
        string codigo,
        int estado);
}
