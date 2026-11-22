using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Ascenso.Becas.Api.Logging;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Ascenso.Becas.IntegrationTests.Logging;

public sealed class ConfiguracionSerilogTests
{
    private const string CorrelationId = "correlacion-prueba-123";

    [Fact]
    public void Debe_EnriquecerLosEventos_ConServicioCorrelacionYTraza()
    {
        var acceso = CrearAcceso();
        using var logger = CrearLogger(acceso, out var sink);
        using var actividad = new Activity("prueba").Start();

        try
        {
            logger.Information(
                "Oferta emitida para {Correo}",
                "ana@ejemplo.com");
        }
        finally
        {
            actividad.Stop();
        }

        var evento = sink.Unico();

        Valor(evento, EnriquecedorCorrelacion.PropiedadCorrelacion)
            .Should().Be(CorrelationId);
        Valor(evento, EnriquecedorServicio.PropiedadServicio)
            .Should().Be(ConfiguracionSerilog.NombreServicio);
        Valor(evento, EnriquecedorServicio.PropiedadEntorno)
            .Should().Be("Testing");
        Valor(evento, EnriquecedorCorrelacion.PropiedadTraceId)
            .Should().Be(actividad.TraceId.ToHexString());
        Valor(evento, EnriquecedorCorrelacion.PropiedadSpanId)
            .Should().Be(actividad.SpanId.ToHexString());
        Valor(evento, "Correo")
            .Should().Be(RedaccionDatosSensibles.Reemplazo);
    }

    [Fact]
    public void Debe_ProducirJson_ConLasPropiedadesEnriquecidas()
    {
        var acceso = CrearAcceso();
        using var logger = CrearLogger(acceso, out var sink);

        logger.Information(
            "Oferta emitida para {Correo}",
            "ana@ejemplo.com");

        var json = Formatear(sink.Unico());

        using var documento = JsonDocument.Parse(json);
        var raiz = documento.RootElement;

        raiz.GetProperty(EnriquecedorCorrelacion.PropiedadCorrelacion)
            .GetString().Should().Be(CorrelationId);
        raiz.GetProperty(EnriquecedorServicio.PropiedadServicio)
            .GetString().Should().Be(ConfiguracionSerilog.NombreServicio);
        raiz.GetProperty("Correo").GetString()
            .Should().Be(RedaccionDatosSensibles.Reemplazo);
        json.Should().NotContain("ana@ejemplo.com");
        raiz.GetProperty("@mt").GetString()
            .Should().Contain("Oferta emitida");
    }

    [Fact]
    public void Debe_OcultarCadenasDeConexionYSecretos_EnLaSalida()
    {
        var acceso = CrearAcceso();
        using var logger = CrearLogger(acceso, out var sink);

        logger.Information(
            "Conexión {ConnectionString} con {Password} y {ClientSecret}",
            "Host=db;Username=postgres;Password=supersecreto",
            "supersecreto",
            "otro-secreto");

        var json = Formatear(sink.Unico());

        json.Should().Contain(RedaccionDatosSensibles.Reemplazo);
        json.Should().NotContain("supersecreto");
        json.Should().NotContain("otro-secreto");
        json.Should().NotContain("Host=db");
        json.Should().NotContain("Username=postgres");
    }

    private static HttpContextAccessor CrearAcceso()
        => new()
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = CorrelationId
            }
        };

    private static Logger CrearLogger(
        IHttpContextAccessor acceso,
        out SinkEnMemoria sink)
    {
        sink = new SinkEnMemoria();

        var configuracion = new LoggerConfiguration();
        ConfiguracionSerilog.Configurar(
            configuracion,
            new EntornoPrueba(),
            acceso,
            sink);

        return configuracion.CreateLogger();
    }

    private static string Formatear(LogEvent evento)
    {
        using var escritor = new StringWriter(CultureInfo.InvariantCulture);

        ConfiguracionSerilog.FormatoJson.Format(evento, escritor);

        return escritor.ToString();
    }

    private static string? Valor(LogEvent evento, string nombre)
        => evento.Properties.TryGetValue(nombre, out var valor)
            && valor is ScalarValue { Value: { } escalar }
                ? Convert.ToString(escalar, CultureInfo.InvariantCulture)
                : null;

    private sealed class EntornoPrueba : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";

        public string ApplicationName { get; set; } = "Ascenso.Becas.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; }
            = new NullFileProvider();
    }

    private sealed class SinkEnMemoria : ILogEventSink
    {
        private readonly List<LogEvent> _eventos = [];

        public void Emit(LogEvent logEvent) => _eventos.Add(logEvent);

        public LogEvent Unico() => _eventos.Single();
    }
}
