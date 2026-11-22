using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Ascenso.Becas.Api.Logging;

/// <summary>
/// Agrega la correlación de la petición y los identificadores de la traza
/// activa a cada evento de log.
/// </summary>
public sealed class EnriquecedorCorrelacion : ILogEventEnricher
{
    public const string PropiedadCorrelacion = "CorrelationId";

    public const string PropiedadTraceId = "TraceId";

    public const string PropiedadSpanId = "SpanId";

    private readonly IHttpContextAccessor _acceso;

    public EnriquecedorCorrelacion(IHttpContextAccessor acceso)
    {
        _acceso = acceso ?? throw new ArgumentNullException(nameof(acceso));
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var correlacion = _acceso.HttpContext?.TraceIdentifier;

        if (!string.IsNullOrWhiteSpace(correlacion))
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty(PropiedadCorrelacion, correlacion));
        }

        if (Activity.Current is not { } actividad)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(
                PropiedadTraceId,
                actividad.TraceId.ToHexString()));

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(
                PropiedadSpanId,
                actividad.SpanId.ToHexString()));
    }
}
