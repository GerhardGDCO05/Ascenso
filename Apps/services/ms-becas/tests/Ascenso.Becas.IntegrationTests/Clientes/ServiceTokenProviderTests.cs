using System.Net;
using System.Text.Json;
using Ascenso.Becas.Infrastructure.Clientes;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Ascenso.Becas.IntegrationTests.Clientes;

public sealed class ServiceTokenProviderTests
{
    [Fact]
    public async Task Debe_ReutilizarElToken_Cuando_NoHaVencido()
    {
        using var servidor = WireMockServer.Start();
        ConfigurarToken(servidor, "token-cacheado", 3600);
        using var proveedor = CrearProveedor(servidor, margenRenovacion: 30);

        var primero = await proveedor.ObtenerTokenAsync();
        var segundo = await proveedor.ObtenerTokenAsync();

        primero.Should().Be("token-cacheado");
        segundo.Should().Be("token-cacheado");
        ContarSolicitudesDeToken(servidor).Should().Be(1);
    }

    [Fact]
    public async Task Debe_RenovarElToken_Cuando_EstaProximoAVencer()
    {
        using var servidor = WireMockServer.Start();
        ConfigurarToken(servidor, "token-renovado", 60);
        using var proveedor = CrearProveedor(servidor, margenRenovacion: 120);

        await proveedor.ObtenerTokenAsync();
        await proveedor.ObtenerTokenAsync();

        ContarSolicitudesDeToken(servidor).Should().Be(2);
    }

    [Fact]
    public async Task Debe_SolicitarUnSoloToken_Cuando_LasLlamadasSonConcurrentes()
    {
        using var servidor = WireMockServer.Start();
        ConfigurarToken(servidor, "token-concurrente", 3600);
        using var proveedor = CrearProveedor(servidor, margenRenovacion: 30);

        var tareas = Enumerable.Range(0, 10)
            .Select(_ => proveedor.ObtenerTokenAsync())
            .ToArray();

        var tokens = await Task.WhenAll(tareas);

        tokens.Should().OnlyContain(token => token == "token-concurrente");
        ContarSolicitudesDeToken(servidor).Should().Be(1);
    }

    [Fact]
    public async Task Debe_EnviarElTokenEnElEncabezadoAuthorization_Cuando_SeUsaElHandler()
    {
        using var servidor = WireMockServer.Start();
        ConfigurarToken(servidor, "token-bearer", 3600);

        servidor
            .Given(Request.Create().WithPath("/recurso").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));

        using var proveedor = CrearProveedor(servidor, margenRenovacion: 30);
        using var manejador = new TokenServicioDelegatingHandler(proveedor)
        {
            InnerHandler = new HttpClientHandler()
        };
        using var cliente = new HttpClient(manejador)
        {
            BaseAddress = new Uri(servidor.Url!)
        };

        var respuesta = await cliente.GetAsync("/recurso");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var peticion = servidor.LogEntries
            .Single(entrada => entrada.RequestMessage!.Path == "/recurso")
            .RequestMessage!;

        peticion.Headers!["Authorization"]!.Single().Should().Be("Bearer token-bearer");
    }

    [Fact]
    public async Task Debe_InvalidarElToken_Cuando_ElProveedorResponde401()
    {
        using var servidor = WireMockServer.Start();
        ConfigurarToken(servidor, "token-invalidado", 3600);

        servidor
            .Given(Request.Create().WithPath("/recurso").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(401));

        using var proveedor = CrearProveedor(servidor, margenRenovacion: 30);
        using var manejador = new TokenServicioDelegatingHandler(proveedor)
        {
            InnerHandler = new HttpClientHandler()
        };
        using var cliente = new HttpClient(manejador)
        {
            BaseAddress = new Uri(servidor.Url!)
        };

        await cliente.GetAsync("/recurso");
        await cliente.GetAsync("/recurso");

        ContarSolicitudesDeToken(servidor).Should().Be(2);
    }

    [Fact]
    public async Task Debe_FallarDeFormaCerrada_Cuando_KeycloakRechazaLasCredenciales()
    {
        using var servidor = WireMockServer.Start();

        servidor
            .Given(Request.Create()
                .WithPath("/protocol/openid-connect/token")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(401));

        using var proveedor = CrearProveedor(servidor, margenRenovacion: 30);

        var accion = () => proveedor.ObtenerTokenAsync();

        await accion.Should().ThrowAsync<HttpRequestException>();
    }

    private static ServiceTokenProvider CrearProveedor(
        WireMockServer servidor,
        int margenRenovacion)
        => new(
            new HttpClient(),
            new ServiceTokenOptions
            {
                TokenEndpoint = $"{servidor.Url}/protocol/openid-connect/token",
                ClientId = "ms-bec",
                ClientSecret = "secreto-de-prueba",
                MargenRenovacionSegundos = margenRenovacion,
                TimeoutSegundos = 3
            });

    private static void ConfigurarToken(
        WireMockServer servidor,
        string token,
        int expiraEnSegundos)
    {
        servidor
            .Given(Request.Create()
                .WithPath("/protocol/openid-connect/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    access_token = token,
                    expires_in = expiraEnSegundos,
                    token_type = "Bearer"
                })));
    }

    private static int ContarSolicitudesDeToken(WireMockServer servidor)
        => servidor.LogEntries.Count(entrada =>
            entrada.RequestMessage!.Path == "/protocol/openid-connect/token");
}
