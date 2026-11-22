using Ascenso.Becas.Infrastructure.Metricas;
using Ascenso.Becas.Infrastructure.Observabilidad;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Ascenso.Becas.Api.Observabilidad;

/// <summary>
/// Registra OpenTelemetry para ASP.NET Core, HttpClient, Npgsql, MassTransit
/// y Hangfire. El exportador OTLP solo se agrega cuando hay un endpoint
/// configurado; en pruebas se puede inyectar un exportador propio.
/// </summary>
public static class RegistroTelemetria
{
    public const string FuenteNpgsql = "Npgsql";

    public const string FuenteMassTransit = "MassTransit";

    public const string MedidorNpgsql = "Npgsql";

    public const string MedidorMassTransit = "MassTransit";

    public const string MedidorAspNetCore = "Microsoft.AspNetCore.Hosting";

    public const string MedidorKestrel = "Microsoft.AspNetCore.Server.Kestrel";

    public const string MedidorHttp = "System.Net.Http";

    public static IServiceCollection AgregarTelemetria(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IHostEnvironment entorno)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(entorno);

        var opciones = OpcionesTelemetria.Leer(configuracion);

        if (!opciones.Habilitada)
        {
            servicios.AddSingleton(opciones);
            return servicios;
        }

        servicios.AddSingleton(opciones);

        servicios
            .AddOpenTelemetry()
            .ConfigureResource(recurso => recurso
                .AddService(
                    opciones.NombreServicio,
                    serviceVersion: VersionServicio(),
                    serviceInstanceId: entorno.ApplicationName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = entorno.EnvironmentName
                }))
            .WithTracing(trazas =>
            {
                trazas
                    .AddSource(TelemetriaHangfire.NombreFuente)
                    .AddSource(FuenteNpgsql)
                    .AddSource(FuenteMassTransit)
                    .AddAspNetCoreInstrumentation(instrumentacion =>
                    {
                        instrumentacion.Filter = contexto =>
                            !contexto.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation();

                if (opciones.EndpointOtlp is not null)
                {
                    trazas.AddOtlpExporter(exportador =>
                        ConfigurarExportador(exportador, opciones));
                }
            })
            .WithMetrics(metricas =>
            {
                metricas
                    .AddMeter(MetricasOtorgamiento.NombreMedidor)
                    .AddMeter(MetricaConfiguracionFaltante.NombreMedidor)
                    .AddMeter(MedidorNpgsql)
                    .AddMeter(MedidorMassTransit)
                    .AddMeter(MedidorAspNetCore)
                    .AddMeter(MedidorKestrel)
                    .AddMeter(MedidorHttp)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (opciones.EndpointOtlp is not null)
                {
                    metricas.AddOtlpExporter((exportador, lector) =>
                    {
                        ConfigurarExportador(exportador, opciones);
                        lector.PeriodicExportingMetricReaderOptions
                            .ExportIntervalMilliseconds = (int)Math.Max(
                                1000,
                                opciones.IntervaloExportacion.TotalMilliseconds);
                    });
                }
            });

        if (opciones.MetricasPendientesHabilitadas)
        {
            servicios.AddHostedService<PublicadorMetricasPendientes>();
        }

        return servicios;
    }

    private static void ConfigurarExportador(
        OtlpExporterOptions exportador,
        OpcionesTelemetria opciones)
    {
        exportador.Endpoint = opciones.EndpointOtlp!;
        exportador.Protocol = opciones.Protocolo;
    }

    private static string VersionServicio()
        => typeof(RegistroTelemetria).Assembly
            .GetName()
            .Version?
            .ToString() ?? "desconocida";
}
