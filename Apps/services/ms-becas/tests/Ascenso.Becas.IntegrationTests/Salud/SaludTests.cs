using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Ascenso.Becas.Api.Salud;
using Ascenso.Becas.Infrastructure.Persistence;
using Ascenso.Becas.IntegrationTests.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ascenso.Becas.IntegrationTests.Salud;

public sealed class SaludTests : IClassFixture<OfertasBecaApiFactory>
{
    private readonly OfertasBecaApiFactory _fabricaBase;

    public SaludTests(OfertasBecaApiFactory fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_Responder200_EnLiveness_SinEjecutarDependencias()
    {
        using var fabrica = CrearFabricaConChequeos(
            new ChequeoFijo(
                HealthStatus.Unhealthy,
                "PostgreSQL detenido."),
            new ChequeoFijo(
                HealthStatus.Unhealthy,
                "RabbitMQ detenido."));
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/health/live");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        respuesta.Content.Headers.ContentType?.MediaType
            .Should().Be("application/json");

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;

        raiz.GetProperty("status").GetString().Should().Be("Healthy");
        raiz.GetProperty("checks").EnumerateArray()
            .Should().BeEmpty("liveness no debe depender de PostgreSQL ni de RabbitMQ.");
    }

    [Fact]
    public async Task Debe_Responder200_EnReadiness_Cuando_LasDependenciasEstanSanas()
    {
        using var fabrica = CrearFabricaConChequeos(
            new ChequeoFijo(HealthStatus.Healthy, "PostgreSQL disponible."),
            new ChequeoFijo(HealthStatus.Healthy, "RabbitMQ disponible."));
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/health/ready");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;

        raiz.GetProperty("status").GetString().Should().Be("Healthy");

        var chequeos = raiz.GetProperty("checks")
            .EnumerateArray()
            .ToDictionary(
                chequeo => chequeo.GetProperty("name").GetString()!,
                chequeo => chequeo.GetProperty("status").GetString());

        chequeos.Should().ContainKeys(
            PostgresHealthCheck.Nombre,
            RabbitMqHealthCheck.Nombre);
        chequeos.Values.Should().OnlyContain(estado => estado == "Healthy");
    }

    [Fact]
    public async Task Debe_Responder503_EnReadiness_Cuando_PostgresNoEstaDisponible()
    {
        using var fabrica = CrearFabricaConChequeos(
            new ChequeoFijo(
                HealthStatus.Unhealthy,
                "No se pudo conectar a PostgreSQL."),
            new ChequeoFijo(HealthStatus.Healthy, "RabbitMQ disponible."));
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/health/ready");

        respuesta.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;

        raiz.GetProperty("status").GetString().Should().Be("Unhealthy");

        var chequeos = raiz.GetProperty("checks")
            .EnumerateArray()
            .ToDictionary(
                chequeo => chequeo.GetProperty("name").GetString()!,
                chequeo => chequeo.GetProperty("status").GetString());

        chequeos[PostgresHealthCheck.Nombre].Should().Be("Unhealthy");
        chequeos[RabbitMqHealthCheck.Nombre].Should().Be("Healthy");

        var liveness = await cliente.GetAsync("/health/live");
        liveness.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "liveness no debe fallar por una dependencia caída.");
    }

    [Fact]
    public async Task Debe_Responder503_EnReadiness_Cuando_PostgresRealEstaInalcanzable()
    {
        using var fabrica = _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<DbContextOptions<BecasDbContext>>();
                servicios.AddScoped(_ =>
                    new DbContextOptionsBuilder<BecasDbContext>()
                        .UseNpgsql(
                            "Host=127.0.0.1;Port=1;Database=ascenso_becas_tests;"
                            + "Username=postgres;Password=postgres;"
                            + "Timeout=2;Pooling=false")
                        .Options);
            });
        });
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/health/ready");

        respuesta.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var contenido = await respuesta.Content.ReadAsStringAsync();
        contenido.Should().NotContain("Password=");
        contenido.Should().NotContain("Username=");

        using var documento = JsonDocument.Parse(contenido);
        var raiz = documento.RootElement;

        raiz.GetProperty("status").GetString().Should().Be("Unhealthy");

        var chequeos = raiz.GetProperty("checks")
            .EnumerateArray()
            .ToDictionary(
                chequeo => chequeo.GetProperty("name").GetString()!,
                chequeo => chequeo.GetProperty("status").GetString());

        chequeos[PostgresHealthCheck.Nombre].Should().Be("Unhealthy");
        chequeos[RabbitMqHealthCheck.Nombre].Should().Be(
            "Healthy",
            "la mensajería está deshabilitada en este despliegue.");
    }

    [Fact]
    public async Task Debe_EstarSano_RabbitMq_Cuando_LaMensajeriaEstaDeshabilitada()
    {
        var chequeo = new RabbitMqHealthCheck(CrearConfiguracionRabbitMq(
            habilitada: "false",
            puerto: 1));

        var resultado = await chequeo.CheckHealthAsync(
            new HealthCheckContext());

        resultado.Status.Should().Be(HealthStatus.Healthy);
        resultado.Description.Should().Contain("deshabilitada");
    }

    [Fact]
    public async Task Debe_EstarSano_RabbitMq_Cuando_AceptaConexiones()
    {
        var escucha = new TcpListener(IPAddress.Loopback, 0);
        escucha.Start();

        try
        {
            var puerto = ((IPEndPoint)escucha.LocalEndpoint).Port;
            var chequeo = new RabbitMqHealthCheck(CrearConfiguracionRabbitMq(
                habilitada: "true",
                puerto: puerto));

            var resultado = await chequeo.CheckHealthAsync(
                new HealthCheckContext());

            resultado.Status.Should().Be(HealthStatus.Healthy);
        }
        finally
        {
            escucha.Stop();
        }
    }

    [Fact]
    public async Task Debe_EstarNoSano_RabbitMq_Cuando_NoResponde()
    {
        var chequeo = new RabbitMqHealthCheck(CrearConfiguracionRabbitMq(
            habilitada: "true",
            puerto: 1));

        var resultado = await chequeo.CheckHealthAsync(
            new HealthCheckContext());

        resultado.Status.Should().Be(HealthStatus.Unhealthy);
        resultado.Description.Should().Contain("RabbitMQ");
    }

    [Fact]
    public async Task Debe_EstarNoSano_Postgres_Cuando_NoResponde()
    {
        var servicios = new ServiceCollection();
        servicios.AddDbContext<BecasDbContext>(opciones =>
            opciones.UseNpgsql(
                "Host=127.0.0.1;Port=1;Database=ascenso_becas_tests;"
                + "Username=postgres;Password=postgres;"
                + "Timeout=2;Pooling=false"));

        await using var proveedor = servicios.BuildServiceProvider();
        var chequeo = new PostgresHealthCheck(
            proveedor.GetRequiredService<IServiceScopeFactory>());

        var resultado = await chequeo.CheckHealthAsync(
            new HealthCheckContext());

        resultado.Status.Should().Be(HealthStatus.Unhealthy);
        resultado.Description.Should().Contain("PostgreSQL");
    }

    private WebApplicationFactory<Program> CrearFabricaConChequeos(
        IHealthCheck postgres,
        IHealthCheck rabbitMq)
        => _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.Configure<HealthCheckServiceOptions>(opciones =>
                {
                    opciones.Registrations.Clear();
                    opciones.Registrations.Add(new HealthCheckRegistration(
                        PostgresHealthCheck.Nombre,
                        postgres,
                        HealthStatus.Unhealthy,
                        [RegistroSalud.EtiquetaReadiness]));
                    opciones.Registrations.Add(new HealthCheckRegistration(
                        RabbitMqHealthCheck.Nombre,
                        rabbitMq,
                        HealthStatus.Unhealthy,
                        [RegistroSalud.EtiquetaReadiness]));
                });
            });
        });

    private static IConfiguration CrearConfiguracionRabbitMq(
        string habilitada,
        int puerto)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Mensajeria:Habilitada"] = habilitada,
                    ["RabbitMq:Host"] = "127.0.0.1",
                    ["RabbitMq:Port"] = puerto.ToString(
                        CultureInfo.InvariantCulture)
                })
            .Build();

    private sealed class ChequeoFijo : IHealthCheck
    {
        private readonly HealthStatus _estado;
        private readonly string _descripcion;

        public ChequeoFijo(HealthStatus estado, string descripcion)
        {
            _estado = estado;
            _descripcion = descripcion;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new HealthCheckResult(_estado, _descripcion));
    }
}
