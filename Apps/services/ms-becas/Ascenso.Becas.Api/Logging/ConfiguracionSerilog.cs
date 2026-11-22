using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

namespace Ascenso.Becas.Api.Logging;

/// <summary>
/// Configura Serilog con salida JSON, enriquecimiento de servicio y
/// correlación, y redacción de datos sensibles. La misma configuración se usa
/// en todos los entornos; el nivel mínimo de ASP.NET Core se reduce para no
/// inundar la consola.
/// </summary>
public static class ConfiguracionSerilog
{
    public const string NombreServicio = "ms-bec";

    /// <summary>
    /// Formato JSON compacto usado por la consola. Se expone para que las
    /// pruebas verifiquen la salida real de los eventos enriquecidos.
    /// </summary>
    public static readonly ITextFormatter FormatoJson = new CompactJsonFormatter();

    public static void Configurar(
        LoggerConfiguration configuracion,
        IHostEnvironment entorno,
        IHttpContextAccessor acceso,
        ILogEventSink? sinkAdicional = null)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(entorno);
        ArgumentNullException.ThrowIfNull(acceso);

        configuracion
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With(new EnriquecedorServicio(
                NombreServicio,
                entorno.EnvironmentName))
            .Enrich.With(new EnriquecedorCorrelacion(acceso))
            .Enrich.With<RedaccionLogEventEnricher>()
            .Destructure.With<RedaccionDestructuringPolicy>()
            .WriteTo.Console(FormatoJson);

        if (sinkAdicional is not null)
        {
            configuracion.WriteTo.Sink(sinkAdicional);
        }
    }
}
