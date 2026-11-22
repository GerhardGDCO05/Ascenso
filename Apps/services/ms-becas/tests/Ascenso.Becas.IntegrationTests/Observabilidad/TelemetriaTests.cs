using System.Diagnostics;
using Ascenso.Becas.Api.Observabilidad;
using Ascenso.Becas.Infrastructure.Metricas;
using Ascenso.Becas.Infrastructure.Observabilidad;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Ascenso.Becas.IntegrationTests.Observabilidad;

[Collection(ColeccionMetricas.Nombre)]
public sealed class TelemetriaTests
{
    [Fact]
    public void Debe_LeerLasOpcionesConfiguradas()
    {
        var configuracion = CrearConfiguracion(
            endpoint: "http://localhost:4317",
            protocolo: "HttpProtobuf",
            intervaloSegundos: "5");

        var opciones = OpcionesTelemetria.Leer(configuracion);

        opciones.Habilitada.Should().BeTrue();
        opciones.NombreServicio.Should().Be("ms-bec-pruebas");
        opciones.EndpointOtlp.Should().Be(new Uri("http://localhost:4317"));
        opciones.Protocolo.Should().Be(OtlpExportProtocol.HttpProtobuf);
        opciones.IntervaloExportacion.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Debe_UsarValoresPorDefecto_CuandoFaltaLaConfiguracion()
    {
        var configuracion = new ConfigurationBuilder().Build();

        var opciones = OpcionesTelemetria.Leer(configuracion);

        opciones.Habilitada.Should().BeTrue();
        opciones.NombreServicio.Should().Be("ms-bec");
        opciones.EndpointOtlp.Should().BeNull();
        opciones.Protocolo.Should().Be(OtlpExportProtocol.Grpc);
        opciones.IntervaloExportacion.Should().Be(TimeSpan.FromSeconds(10));
        opciones.MetricasPendientesHabilitadas.Should().BeTrue();
        opciones.IntervaloMetricasPendientes
            .Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Debe_RechazarUnEndpointOtlpInvalido()
    {
        var configuracion = CrearConfiguracion(endpoint: "no-es-una-uri");

        var accion = () => OpcionesTelemetria.Leer(configuracion);

        accion.Should().Throw<InvalidOperationException>()
            .WithMessage("*OTLP*");
    }

    [Fact]
    public void Debe_RegistrarElPublicador_SoloCuandoEstaHabilitado()
    {
        var servicios = new ServiceCollection();

        RegistroTelemetria.AgregarTelemetria(
            servicios,
            CrearConfiguracion(metricasPendientes: false),
            new EntornoPrueba());

        servicios
            .Any(descriptor =>
                descriptor.ImplementationType
                    == typeof(PublicadorMetricasPendientes))
            .Should().BeFalse();

        var serviciosHabilitados = new ServiceCollection();

        RegistroTelemetria.AgregarTelemetria(
            serviciosHabilitados,
            CrearConfiguracion(metricasPendientes: true),
            new EntornoPrueba());

        serviciosHabilitados
            .Any(descriptor =>
                descriptor.ImplementationType
                    == typeof(PublicadorMetricasPendientes))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Debe_ExportarTrazasYMetricas_ConElPipelineConfigurado()
    {
        var exportadorActividades = new ExportadorActividades();
        var exportadorMetricas = new ExportadorMetricas();
        var servicios = new ServiceCollection();

        RegistroTelemetria.AgregarTelemetria(
            servicios,
            CrearConfiguracion(),
            new EntornoPrueba());

        servicios
            .AddOpenTelemetry()
            .WithTracing(trazas => trazas.AddProcessor(
                new ProcesadorActividades(exportadorActividades)))
            .WithMetrics(metricas => metricas.AddReader(
                new LectorMetricas(exportadorMetricas)));

        await using var proveedor = servicios.BuildServiceProvider();
        var trazador = proveedor.GetRequiredService<TracerProvider>();
        var medidor = proveedor.GetRequiredService<MeterProvider>();

        await TelemetriaHangfire.EjecutarAsync(
            "PruebaJob",
            () => Task.CompletedTask);

        var metricas = new MetricasOtorgamiento();
        metricas.OfertaEmitida();
        metricas.OfertaAceptada();
        metricas.OfertaRechazada();
        metricas.OfertaExpirada();
        MetricasOtorgamiento.ActualizarPendientes(5, 2);

        trazador.ForceFlush();
        medidor.ForceFlush();

        var actividad = exportadorActividades.Actividades
            .Single(item => item.DisplayName == "hangfire PruebaJob");

        actividad.Source.Name.Should().Be(TelemetriaHangfire.NombreFuente);
        actividad.GetTagItem(TelemetriaHangfire.PropiedadTrabajo)
            .Should().Be("PruebaJob");
        actividad.Status.Should().Be(ActivityStatusCode.Unset);

        exportadorMetricas.Metricas
            .Select(metrica => metrica.Name)
            .Distinct()
            .Should()
            .Contain(MetricasOtorgamiento.NombreOfertasEmitidas)
            .And.Contain(MetricasOtorgamiento.NombreOfertasAceptadas)
            .And.Contain(MetricasOtorgamiento.NombreOfertasRechazadas)
            .And.Contain(MetricasOtorgamiento.NombreOfertasExpiradas)
            .And.Contain(MetricasOtorgamiento.NombreCompromisosPendientes)
            .And.Contain(MetricasOtorgamiento.NombreOutboxPendientes);
    }

    private static IConfiguration CrearConfiguracion(
        bool habilitada = true,
        string? endpoint = null,
        string? protocolo = null,
        string? intervaloSegundos = null,
        bool metricasPendientes = false)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Observabilidad:Telemetria:Habilitada"]
                        = habilitada.ToString(),
                    ["Observabilidad:Telemetria:NombreServicio"]
                        = "ms-bec-pruebas",
                    ["Observabilidad:Telemetria:EndpointOtlp"] = endpoint,
                    ["Observabilidad:Telemetria:Protocolo"] = protocolo,
                    ["Observabilidad:Telemetria:IntervaloExportacionSegundos"]
                        = intervaloSegundos,
                    ["Observabilidad:MetricasPendientes:Habilitada"]
                        = metricasPendientes.ToString(),
                    ["Observabilidad:MetricasPendientes:IntervaloSegundos"] = "1"
                })
            .Build();

    private sealed class EntornoPrueba : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";

        public string ApplicationName { get; set; } = "Ascenso.Becas.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; }
            = new NullFileProvider();
    }

    private sealed class ProcesadorActividades : BaseProcessor<Activity>
    {
        private readonly ExportadorActividades _exportador;

        public ProcesadorActividades(ExportadorActividades exportador)
        {
            _exportador = exportador;
        }

        public override void OnEnd(Activity data)
            => _exportador.Agregar(data);
    }

    private sealed class ExportadorActividades : BaseExporter<Activity>
    {
        private readonly List<Activity> _actividades = [];

        public IReadOnlyList<Activity> Actividades
        {
            get
            {
                lock (_actividades)
                {
                    return _actividades.ToArray();
                }
            }
        }

        public void Agregar(Activity actividad)
        {
            lock (_actividades)
            {
                _actividades.Add(actividad);
            }
        }

        public override ExportResult Export(in Batch<Activity> batch)
            => ExportResult.Success;
    }

    private sealed class ExportadorMetricas : BaseExporter<Metric>
    {
        private readonly List<Metric> _metricas = [];

        public IReadOnlyList<Metric> Metricas
        {
            get
            {
                lock (_metricas)
                {
                    return _metricas.ToArray();
                }
            }
        }

        public override ExportResult Export(in Batch<Metric> batch)
        {
            lock (_metricas)
            {
                foreach (var metrica in batch)
                {
                    _metricas.Add(metrica);
                }
            }

            return ExportResult.Success;
        }
    }

    private sealed class LectorMetricas : BaseExportingMetricReader
    {
        public LectorMetricas(BaseExporter<Metric> exportador)
            : base(exportador)
        {
        }
    }
}
