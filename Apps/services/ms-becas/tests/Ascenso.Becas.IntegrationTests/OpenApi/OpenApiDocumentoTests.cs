using System.Net;
using System.Text.Json;
using Ascenso.Becas.IntegrationTests.Controllers;
using FluentAssertions;
using Microsoft.OpenApi;

namespace Ascenso.Becas.IntegrationTests.OpenApi;

public sealed class OpenApiDocumentoTests : IClassFixture<OfertasBecaApiFactory>
{
    private static readonly string[] _rutasEsperadas =
    [
        "/api/v1/becas/ofertas",
        "/api/v1/becas/ofertas/{ofertaId}",
        "/api/v1/becas/ofertas/{ofertaId}/aceptar",
        "/api/v1/becas/ofertas/{ofertaId}/rechazar",
        "/api/v1/becas/ofertas/{ofertaId}/anular",
        "/api/v1/becas/{becaId}",
        "/api/v1/becas/{becaId}/compromisos",
        "/api/v1/becas/compromisos/{compromisoId}/revision",
        "/internal/v1/becas/activas"
    ];

    private readonly OfertasBecaApiFactory _fabrica;

    public OpenApiDocumentoTests(OfertasBecaApiFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Debe_PublicarElDocumentoSwagger_ValidoYConTodasLasRutas()
    {
        using var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var contenido = await respuesta.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(contenido);
        var raiz = documento.RootElement;

        var lectura = OpenApiDocument.Parse(contenido, "json");
        lectura.Document.Should().NotBeNull(
            "el documento OpenAPI publicado debe ser parseable.");
        lectura.Diagnostic.Should().NotBeNull();
        lectura.Diagnostic!.Errors.Should().BeEmpty(
            "el documento OpenAPI publicado no debe tener errores de formato.");

        raiz.GetProperty("openapi").GetString()
            .Should().StartWith("3.");
        raiz.GetProperty("info").GetProperty("title").GetString()
            .Should().NotBeNullOrWhiteSpace();

        var rutas = raiz.GetProperty("paths");

        foreach (var ruta in _rutasEsperadas)
        {
            rutas.TryGetProperty(ruta, out _).Should().BeTrue(
                $"el documento OpenAPI debe contener la ruta {ruta}.");
        }
    }

    [Fact]
    public async Task Debe_DeclararElEsquemaDeSeguridadBearer_YAplicarloGlobalmente()
    {
        using var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");
        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;

        var esquemas = raiz
            .GetProperty("components")
            .GetProperty("securitySchemes");
        var bearer = esquemas.GetProperty("Bearer");

        bearer.GetProperty("type").GetString().Should().Be("http");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
        bearer.GetProperty("bearerFormat").GetString().Should().Be("JWT");

        var seguridad = raiz.GetProperty("security");
        seguridad.EnumerateArray()
            .Any(requisito => requisito.TryGetProperty("Bearer", out _))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Debe_DocumentarIdempotencyKey_EnTodosLosComandosPost()
    {
        using var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");
        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var rutas = documento.RootElement.GetProperty("paths");

        var operacionesPost = rutas
            .EnumerateObject()
            .SelectMany(ruta => ruta.Value
                .EnumerateObject()
                .Where(operacion => operacion.NameEquals("post"))
                .Select(operacion => (Ruta: ruta.Name, Operacion: operacion.Value)))
            .ToArray();

        operacionesPost.Should().NotBeEmpty();

        foreach (var (ruta, operacion) in operacionesPost)
        {
            var parametro = operacion
                .GetProperty("parameters")
                .EnumerateArray()
                .SingleOrDefault(candidato =>
                    candidato.TryGetProperty("name", out var nombre)
                    && nombre.GetString() == "Idempotency-Key");

            parametro.ValueKind.Should().NotBe(
                JsonValueKind.Undefined,
                $"POST {ruta} debe documentar Idempotency-Key.");
            parametro.GetProperty("in").GetString().Should().Be("header");
            parametro.GetProperty("required").GetBoolean().Should().BeTrue();

            var esquema = parametro.GetProperty("schema");
            esquema.GetProperty("minLength").GetInt32().Should().Be(8);
            esquema.GetProperty("maxLength").GetInt32().Should().Be(128);
        }
    }

    [Fact]
    public async Task Debe_IncluirEjemplosDeSolicitudYRespuesta_EnLasOperacionesPrincipales()
    {
        using var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");
        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var rutas = documento.RootElement.GetProperty("paths");

        var emision = rutas
            .GetProperty("/api/v1/becas/ofertas")
            .GetProperty("post");

        emision.GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example")
            .TryGetProperty("postulacionId", out _)
            .Should().BeTrue();

        emision.GetProperty("responses")
            .GetProperty("201")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example")
            .TryGetProperty("ofertaId", out _)
            .Should().BeTrue();

        var revision = rutas
            .GetProperty("/api/v1/becas/compromisos/{compromisoId}/revision")
            .GetProperty("post");

        revision.GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example")
            .TryGetProperty("aprobar", out _)
            .Should().BeTrue();

        var activas = rutas
            .GetProperty("/internal/v1/becas/activas")
            .GetProperty("get");

        activas.GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example")
            .ValueKind.Should().Be(JsonValueKind.Array);
    }
}
