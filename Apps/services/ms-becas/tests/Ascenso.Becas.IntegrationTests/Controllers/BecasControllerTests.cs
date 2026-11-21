using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerBeca;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Controllers;

public sealed class BecasControllerTests : IClassFixture<OfertasBecaApiFactory>
{
    private const string EncabezadoRoles = "X-Test-Roles";
    private const string EncabezadoPostulante = "X-Test-Postulante";
    private const string EncabezadoProgramas = "X-Test-Programas";
    private const string EncabezadoAcr = "X-Test-Acr";

    private readonly OfertasBecaApiFactory _fabricaBase;

    public BecasControllerTests(OfertasBecaApiFactory fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_ExponerLaRutaDeBeca_ConLosCodigosDeRespuesta_EnOpenApi()
    {
        using var cliente = _fabricaBase.CreateClient();

        var respuesta = await cliente.GetAsync("/openapi/v1.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var rutas = documento.RootElement.GetProperty("paths");

        rutas.TryGetProperty(
                "/api/v1/becas/{becaId}",
                out var operaciones)
            .Should().BeTrue("OpenAPI debe exponer la ruta de consulta de beca.");

        operaciones.TryGetProperty("get", out var operacion).Should().BeTrue(
            "OpenAPI debe exponer el método GET de consulta de beca.");

        operacion.TryGetProperty("responses", out var respuestas).Should().BeTrue();

        foreach (var codigo in new[] { "200", "400", "401", "403", "404" })
        {
            respuestas.TryGetProperty(codigo, out _).Should().BeTrue(
                $"OpenAPI debe declarar el código {codigo} para la consulta de beca.");
        }
    }

    [Fact]
    public async Task Debe_ConsultarBeca_Cuando_ElPostulanteEsDueno()
    {
        var becaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var postulanteId = Guid.NewGuid();
        var beca = CrearBeca(becaId, programaId, postulanteId);

        var consultas = Substitute.For<IQueryDispatcher>();
        consultas
            .ConsultarAsync<ObtenerBecaQuery, BecaDto>(
                Arg.Any<ObtenerBecaQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(beca));

        var consultasBeca = CrearConsultasBeca(
            becaId,
            programaId,
            postulanteId);

        using var fabrica = CrearFabrica(
            consultas: consultas,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoPostulante(cliente, postulanteId);

        var respuesta = await cliente.GetAsync($"/api/v1/becas/{becaId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<BecaDto>();
        cuerpo.Should().BeEquivalentTo(beca);
        cuerpo!.Compromiso.Should().NotBeNull();
        cuerpo.Compromiso!.UrlPlantillaGenerada.Should().Be(
            "https://storage.example/becas/plantilla-compromiso.pdf");

        await consultas.Received(1).ConsultarAsync<ObtenerBecaQuery, BecaDto>(
            Arg.Is<ObtenerBecaQuery>(consulta => consulta.BecaId == becaId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_ConsultarBeca_Cuando_ElCoordinadorTieneAlcance()
    {
        var becaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var postulanteId = Guid.NewGuid();
        var beca = CrearBeca(becaId, programaId, postulanteId);

        var consultas = Substitute.For<IQueryDispatcher>();
        consultas
            .ConsultarAsync<ObtenerBecaQuery, BecaDto>(
                Arg.Any<ObtenerBecaQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(beca));

        var consultasBeca = CrearConsultasBeca(
            becaId,
            programaId,
            postulanteId);

        using var fabrica = CrearFabrica(
            consultas: consultas,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaId);

        var respuesta = await cliente.GetAsync($"/api/v1/becas/{becaId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElPostulanteConsultaUnaBecaAjena()
    {
        var becaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var consultas = Substitute.For<IQueryDispatcher>();

        var consultasBeca = CrearConsultasBeca(
            becaId,
            programaId,
            Guid.NewGuid());

        using var fabrica = CrearFabrica(
            consultas: consultas,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoPostulante(cliente, Guid.NewGuid());

        var respuesta = await cliente.GetAsync($"/api/v1/becas/{becaId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await consultas.DidNotReceive().ConsultarAsync<ObtenerBecaQuery, BecaDto>(
            Arg.Any<ObtenerBecaQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElCoordinadorNoTieneAlcance()
    {
        var becaId = Guid.NewGuid();
        var programaDeLaBeca = Guid.NewGuid();
        var programaDelToken = Guid.NewGuid();
        var consultas = Substitute.For<IQueryDispatcher>();

        var consultasBeca = CrearConsultasBeca(
            becaId,
            programaDeLaBeca,
            Guid.NewGuid());

        using var fabrica = CrearFabrica(
            consultas: consultas,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaDelToken);

        var respuesta = await cliente.GetAsync($"/api/v1/becas/{becaId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await consultas.DidNotReceive().ConsultarAsync<ObtenerBecaQuery, BecaDto>(
            Arg.Any<ObtenerBecaQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder404_Cuando_LaBecaNoExiste()
    {
        var becaId = Guid.NewGuid();
        var consultas = Substitute.For<IQueryDispatcher>();

        using var fabrica = CrearFabrica(consultas: consultas);
        using var cliente = fabrica.CreateClient();
        AutenticarComoPostulante(cliente, Guid.NewGuid());

        var respuesta = await cliente.GetAsync($"/api/v1/becas/{becaId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await consultas.DidNotReceive().ConsultarAsync<ObtenerBecaQuery, BecaDto>(
            Arg.Any<ObtenerBecaQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder401_Cuando_NoHayAutenticacion()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync($"/api/v1/becas/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IQueryDispatcher? consultas = null,
        IConsultasBeca? consultasBeca = null)
        => _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<IQueryDispatcher>();
                servicios.AddSingleton(
                    consultas ?? Substitute.For<IQueryDispatcher>());

                servicios.RemoveAll<IConsultasBeca>();
                servicios.AddSingleton(
                    consultasBeca ?? Substitute.For<IConsultasBeca>());
            });
        });

    private static IConsultasBeca CrearConsultasBeca(
        Guid becaId,
        Guid programaId,
        Guid postulanteId)
    {
        var consultas = Substitute.For<IConsultasBeca>();
        consultas
            .ObtenerRecursoBecaAsync(
                becaId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RecursoBecaDto?>(
                new RecursoBecaDto(
                    becaId,
                    programaId,
                    postulanteId)));

        return consultas;
    }

    private static BecaDto CrearBeca(
        Guid becaId,
        Guid programaId,
        Guid postulanteId)
        => new(
            becaId,
            "BEC-2026-0001",
            postulanteId,
            Guid.NewGuid(),
            programaId,
            "Beca de excelencia",
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            10,
            "OTORGADA",
            null,
            new PoliticaCoberturaDto(
                "PARCIAL",
                50m,
                0m,
                "USD"),
            750m,
            0m,
            true,
            true,
            false,
            false,
            new DateOnly(2026, 11, 15),
            null,
            null,
            null,
            new CompromisoBecarioDto(
                Guid.NewGuid(),
                becaId,
                "PENDIENTE_CARGA",
                0,
                "https://storage.example/becas/plantilla-compromiso.pdf",
                null,
                null,
                null,
                null,
                null),
            []);

    private static void AutenticarComoPostulante(
        HttpClient cliente,
        Guid postulanteId)
    {
        cliente.DefaultRequestHeaders.Add(
            EncabezadoRoles,
            "POSTULANTE");
        cliente.DefaultRequestHeaders.Add(
            EncabezadoPostulante,
            postulanteId.ToString());
    }

    private static void AutenticarComoCoordinador(
        HttpClient cliente,
        Guid programaId)
    {
        cliente.DefaultRequestHeaders.Add(
            EncabezadoRoles,
            "COORDINADOR_PROGRAMA");
        cliente.DefaultRequestHeaders.Add(
            EncabezadoProgramas,
            programaId.ToString());
        cliente.DefaultRequestHeaders.Add(
            EncabezadoAcr,
            "urn:ascenso:loa:2fa");
    }
}
