using System.Text.Json;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Infrastructure.Integraciones;
using FluentAssertions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Ascenso.Programas.IntegrationTests.Integraciones;

public sealed class PeriodoAcademicoClientTests
{
    [Theory]
    [InlineData("VIGENTE")]
    [InlineData("CERRADO")]
    public async Task Debe_DeserializarElContratoDePeriodo(string estado)
    {
        using var servidor = WireMockServer.Start();
        using var httpClient = new HttpClient();
        var periodoId = Guid.Parse("01966000-0000-7000-8000-000000000601");
        servidor.Given(Request.Create()
                .WithPath($"/internal/v1/periodos-academicos/{periodoId:D}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = periodoId,
                    nombre = "2026-II",
                    estado,
                    fechaInicio = "2026-08-01",
                    fechaFin = "2026-12-31"
                })));

        var resultado = await CrearClient(servidor, httpClient).ObtenerPorIdAsync(periodoId.ToString("D"));

        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(periodoId);
        resultado.Nombre.Should().Be("2026-II");
        resultado.Estado.Should().Be(estado);
    }

    [Fact]
    public async Task Debe_DevolverNulo_CuandoElPeriodoNoExiste()
    {
        using var servidor = WireMockServer.Start();
        using var httpClient = new HttpClient();
        var periodoId = Guid.Parse("01966000-0000-7000-8000-000000000602");
        servidor.Given(Request.Create()
                .WithPath($"/internal/v1/periodos-academicos/{periodoId:D}")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var resultado = await CrearClient(servidor, httpClient).ObtenerPorIdAsync(periodoId.ToString("D"));

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Debe_FallarCerrado_CuandoMsPltRespondeConError()
    {
        using var servidor = WireMockServer.Start();
        using var httpClient = new HttpClient();
        var periodoId = Guid.Parse("01966000-0000-7000-8000-000000000603");
        servidor.Given(Request.Create()
                .WithPath($"/internal/v1/periodos-academicos/{periodoId:D}")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var accion = () => CrearClient(servidor, httpClient).ObtenerPorIdAsync(periodoId.ToString("D"));

        var excepcion = await accion.Should().ThrowAsync<ServicioPeriodoAcademicoNoDisponibleException>();
        excepcion.Which.EstadoHttp.Should().Be(503);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"id\":\"01966000-0000-7000-8000-000000000604\",\"nombre\":\"\",\"estado\":\"VIGENTE\"}")]
    [InlineData("{\"id\":\"01966000-0000-7000-8000-000000000605\",\"nombre\":\"2026-II\",\"estado\":\"VIGENTE\"}")]
    public async Task Debe_FallarCerrado_CuandoLaRespuestaEsInvalida(string cuerpo)
    {
        using var servidor = WireMockServer.Start();
        using var httpClient = new HttpClient();
        var periodoId = Guid.Parse("01966000-0000-7000-8000-000000000604");
        servidor.Given(Request.Create()
                .WithPath($"/internal/v1/periodos-academicos/{periodoId:D}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(cuerpo));

        var accion = () => CrearClient(servidor, httpClient).ObtenerPorIdAsync(periodoId.ToString("D"));

        await accion.Should().ThrowAsync<ServicioPeriodoAcademicoNoDisponibleException>();
    }

    private static PeriodoAcademicoClient CrearClient(WireMockServer servidor, HttpClient httpClient)
        => new(
            httpClient,
            Options.Create(new PeriodoAcademicoOptions
            {
                UrlBase = servidor.Url,
                TimeoutSegundos = 5
            }));
}
