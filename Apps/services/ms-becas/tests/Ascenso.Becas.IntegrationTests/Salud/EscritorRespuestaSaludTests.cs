using System.Text.Json;
using Ascenso.Becas.Api.Salud;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Ascenso.Becas.IntegrationTests.Salud;

public sealed class EscritorRespuestaSaludTests
{
    private const string DescripcionSensible =
        "No se pudo conectar. Host=db;Username=postgres;Password=supersecreto";

    [Fact]
    public async Task Debe_OcultarLasDescripciones_EnProduccion()
    {
        var contexto = CrearContexto("Production");

        await EscritorRespuestaSalud.EscribirAsync(
            contexto,
            CrearInforme());

        var contenido = Leer(contexto);

        contenido.Should().NotContain("supersecreto");
        contenido.Should().NotContain("Password=");
        contenido.Should().NotContain("Username=");
        contenido.Should().NotContain("No se pudo conectar");

        using var documento = JsonDocument.Parse(contenido);
        var chequeo = documento.RootElement
            .GetProperty("checks")[0];

        chequeo.GetProperty("status").GetString().Should().Be("Unhealthy");
        chequeo.TryGetProperty("description", out _).Should().BeFalse();
        documento.RootElement.GetProperty("status").GetString()
            .Should().Be("Unhealthy");
    }

    [Fact]
    public async Task Debe_IncluirLasDescripciones_EnDesarrollo()
    {
        var contexto = CrearContexto("Development");

        await EscritorRespuestaSalud.EscribirAsync(
            contexto,
            CrearInforme());

        var contenido = Leer(contexto);

        contenido.Should().Contain("No se pudo conectar");
    }

    private static DefaultHttpContext CrearContexto(string entorno)
    {
        var servicios = new ServiceCollection()
            .AddSingleton<IHostEnvironment>(new EntornoPrueba(entorno))
            .BuildServiceProvider();

        var contexto = new DefaultHttpContext
        {
            RequestServices = servicios
        };

        contexto.Response.Body = new MemoryStream();

        return contexto;
    }

    private static HealthReport CrearInforme()
    {
        var entradas = new Dictionary<string, HealthReportEntry>(
            StringComparer.Ordinal)
        {
            ["postgres"] = new(
                HealthStatus.Unhealthy,
                DescripcionSensible,
                TimeSpan.FromMilliseconds(12),
                exception: null,
                data: null)
        };

        return new HealthReport(entradas, TimeSpan.FromMilliseconds(25));
    }

    private static string Leer(HttpContext contexto)
    {
        contexto.Response.Body.Position = 0;

        using var lector = new StreamReader(
            contexto.Response.Body,
            leaveOpen: true);

        return lector.ReadToEnd();
    }

    private sealed class EntornoPrueba : IHostEnvironment
    {
        public EntornoPrueba(string nombre)
        {
            EnvironmentName = nombre;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "Ascenso.Becas.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; }
            = new NullFileProvider();
    }
}
