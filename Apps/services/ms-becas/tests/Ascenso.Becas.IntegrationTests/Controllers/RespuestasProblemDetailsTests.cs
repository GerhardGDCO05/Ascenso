using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AnularOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Ports;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Controllers;

public sealed class RespuestasProblemDetailsTests
    : IClassFixture<OfertasBecaApiFactory>
{
    private const string EncabezadoIdempotencia = "Idempotency-Key";
    private const string EncabezadoRoles = "X-Test-Roles";
    private const string EncabezadoPostulante = "X-Test-Postulante";
    private const string EncabezadoProgramas = "X-Test-Programas";
    private const string EncabezadoAcr = "X-Test-Acr";

    private readonly OfertasBecaApiFactory _fabricaBase;

    public RespuestasProblemDetailsTests(OfertasBecaApiFactory fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_Responder400_ConProblemDetails_Cuando_ElDtoEsInvalido()
    {
        var consultasBeca = Substitute.For<IConsultasBeca>();

        using var fabrica = CrearFabrica(consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, Guid.NewGuid());
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-problem-400");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/ofertas/{Guid.NewGuid()}/anular",
            new { });

        await VerificarProblemDetailsAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            "SOLICITUD_INVALIDA");

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        documento.RootElement.GetProperty("errors")
            .EnumerateObject()
            .Should().Contain(propiedad =>
                propiedad.Name.Contains(
                    "justificacion",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Debe_Responder401_ConProblemDetails_Cuando_NoHayAutenticacion()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(
            $"/api/v1/becas/{Guid.NewGuid()}");

        await VerificarProblemDetailsAsync(
            respuesta,
            HttpStatusCode.Unauthorized,
            "NO_AUTENTICADO");
    }

    [Fact]
    public async Task Debe_Responder403_ConProblemDetails_Cuando_ElRecursoEsAjeno()
    {
        var becaId = Guid.NewGuid();
        var consultasBeca = Substitute.For<IConsultasBeca>();
        consultasBeca
            .ObtenerRecursoBecaAsync(becaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RecursoBecaDto?>(
                new RecursoBecaDto(
                    becaId,
                    Guid.NewGuid(),
                    Guid.NewGuid())));

        using var fabrica = CrearFabrica(consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoPostulante(cliente, Guid.NewGuid());

        var respuesta = await cliente.GetAsync($"/api/v1/becas/{becaId}");

        await VerificarProblemDetailsAsync(
            respuesta,
            HttpStatusCode.Forbidden,
            "PERMISO_DENEGADO");
    }

    [Fact]
    public async Task Debe_Responder409_ConProblemDetails_Cuando_HayConflictoDeEstado()
    {
        var ofertaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos
            .EnviarAsync(
                Arg.Any<AnularOfertaBecaCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(
                new TransicionEstadoInvalidaException("ACTIVA", "CERRADA")));

        var consultasBeca = CrearConsultasBeca(
            ofertaId,
            programaId,
            Guid.NewGuid());

        using var fabrica = CrearFabrica(
            comandos: comandos,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaId);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-problem-409");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/ofertas/{ofertaId}/anular",
            new { justificacion = "Justificación válida de prueba." });

        await VerificarProblemDetailsAsync(
            respuesta,
            HttpStatusCode.Conflict,
            "TRANSICION_INVALIDA");
    }

    [Fact]
    public async Task Debe_Responder422_ConProblemDetails_Cuando_LaPostulacionNoEsAdjudicable()
    {
        var postulacionId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos
            .EnviarAsync<EmitirOfertaBecaCommand, OfertaEmitidaDto>(
                Arg.Any<EmitirOfertaBecaCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<OfertaEmitidaDto>(
                new PostulacionNoAdjudicableException(postulacionId)));

        var postulaciones = Substitute.For<IPostulacionesClient>();
        postulaciones
            .ObtenerAdjudicabilidadAsync(
                postulacionId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                CrearAdjudicabilidad(postulacionId, programaId)));

        using var fabrica = CrearFabrica(
            comandos: comandos,
            postulaciones: postulaciones);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaId);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-problem-422");

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/becas/ofertas",
            new { postulacionId });

        await VerificarProblemDetailsAsync(
            respuesta,
            HttpStatusCode.UnprocessableEntity,
            "POSTULACION_NO_ADJUDICABLE");
    }

    private WebApplicationFactory<Program> CrearFabrica(
        ICommandDispatcher? comandos = null,
        IQueryDispatcher? consultas = null,
        IConsultasBeca? consultasBeca = null,
        IPostulacionesClient? postulaciones = null)
        => _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<ICommandDispatcher>();
                servicios.AddSingleton(
                    comandos ?? Substitute.For<ICommandDispatcher>());

                servicios.RemoveAll<IQueryDispatcher>();
                servicios.AddSingleton(
                    consultas ?? Substitute.For<IQueryDispatcher>());

                servicios.RemoveAll<IConsultasBeca>();
                servicios.AddSingleton(
                    consultasBeca ?? Substitute.For<IConsultasBeca>());

                servicios.RemoveAll<IPostulacionesClient>();
                servicios.AddSingleton(
                    postulaciones ?? Substitute.For<IPostulacionesClient>());
            });
        });

    private static IConsultasBeca CrearConsultasBeca(
        Guid ofertaId,
        Guid programaId,
        Guid postulanteId)
    {
        var consultas = Substitute.For<IConsultasBeca>();
        consultas
            .ObtenerRecursoOfertaAsync(
                ofertaId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RecursoOfertaDto?>(
                new RecursoOfertaDto(
                    ofertaId,
                    Guid.NewGuid(),
                    programaId,
                    postulanteId)));

        return consultas;
    }

    private static ResultadoAdjudicabilidad CrearAdjudicabilidad(
        Guid postulacionId,
        Guid programaId)
        => new(
            postulacionId,
            true,
            programaId,
            Guid.NewGuid(),
            "2026-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Estudiante de prueba",
            "V-12345678",
            Guid.NewGuid());

    private static async Task VerificarProblemDetailsAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode estadoEsperado,
        string codigoEsperado)
    {
        respuesta.StatusCode.Should().Be(estadoEsperado);
        respuesta.Content.Headers.ContentType?.MediaType
            .Should().Be("application/problem+json");

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;

        raiz.GetProperty("status").GetInt32()
            .Should().Be((int)estadoEsperado);
        raiz.GetProperty("code").GetString()
            .Should().Be(codigoEsperado);
        raiz.GetProperty("title").GetString()
            .Should().NotBeNullOrWhiteSpace();
        raiz.GetProperty("detail").GetString()
            .Should().NotBeNullOrWhiteSpace();
        raiz.GetProperty("instance").GetString()
            .Should().NotBeNullOrWhiteSpace();
        raiz.GetProperty("correlationId").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

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
        Guid programaId,
        bool con2FA = true)
    {
        cliente.DefaultRequestHeaders.Add(
            EncabezadoRoles,
            "COORDINADOR_PROGRAMA");
        cliente.DefaultRequestHeaders.Add(
            EncabezadoProgramas,
            programaId.ToString());

        if (con2FA)
        {
            cliente.DefaultRequestHeaders.Add(
                EncabezadoAcr,
                "urn:ascenso:loa:2fa");
        }
    }
}
