using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ConsultarBecasActivas;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Controllers;

public sealed class InternalBecasControllerTests : IClassFixture<OfertasBecaApiFactory>
{
    private const string EncabezadoCliente = "X-Test-Cliente";
    private const string EncabezadoScopes = "X-Test-Scopes";

    private readonly OfertasBecaApiFactory _fabricaBase;

    public InternalBecasControllerTests(OfertasBecaApiFactory fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_ExponerLaRutaInterna_ConLosCodigosDeRespuesta_EnOpenApi()
    {
        using var cliente = _fabricaBase.CreateClient();

        var respuesta = await cliente.GetAsync("/openapi/v1.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var rutas = documento.RootElement.GetProperty("paths");

        rutas.TryGetProperty("/internal/v1/becas/activas", out var operaciones)
            .Should().BeTrue("OpenAPI debe exponer la consulta interna de becas activas.");

        operaciones.TryGetProperty("get", out var operacion).Should().BeTrue(
            "OpenAPI debe exponer el método GET de la consulta interna.");

        operacion.TryGetProperty("responses", out var respuestas).Should().BeTrue();

        foreach (var codigo in new[] { "200", "400", "401", "403" })
        {
            respuestas.TryGetProperty(codigo, out _).Should().BeTrue(
                $"OpenAPI debe declarar el código {codigo} para la consulta interna.");
        }
    }

    [Fact]
    public async Task Debe_ConsultarLasBecasActivas_Cuando_LaCuentaDeServicioTieneElScope()
    {
        var postulanteId = Guid.NewGuid();
        var periodoId = Guid.NewGuid();
        var becaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var esperado = new List<BecaActivaDto>
        {
            new(
                becaId,
                programaId,
                periodoId,
                "ACTIVA",
                "PARCIAL",
                50m,
                0m,
                "USD",
                false)
        };

        var consultas = Substitute.For<IQueryDispatcher>();
        consultas
            .ConsultarAsync<
                ConsultarBecasActivasQuery,
                IReadOnlyList<BecaActivaDto>>(
                Arg.Any<ConsultarBecasActivasQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BecaActivaDto>>(esperado));

        using var fabrica = CrearFabrica(consultas);
        using var cliente = fabrica.CreateClient();
        AutenticarComoServicio(cliente);

        var respuesta = await cliente.GetAsync(
            $"/internal/v1/becas/activas?postulanteId={postulanteId}&periodoId={periodoId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var elementos = documento.RootElement.EnumerateArray().ToArray();
        elementos.Should().ContainSingle();

        var propiedades = elementos[0]
            .EnumerateObject()
            .Select(propiedad => propiedad.Name)
            .ToArray();

        propiedades.Should().BeEquivalentTo(
            "becaId",
            "programaBecaId",
            "periodoAcademicoId",
            "estado",
            "modalidadCobertura",
            "porcentajeCobertura",
            "montoFijoPeriodo",
            "moneda",
            "permiteAcumulacion");

        propiedades.Should().NotContain(nombre =>
            nombre.Contains("nombre", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("documento", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("cuenta", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("banco", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("correo", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("telefono", StringComparison.OrdinalIgnoreCase));

        await consultas.Received(1).ConsultarAsync<
            ConsultarBecasActivasQuery,
            IReadOnlyList<BecaActivaDto>>(
            Arg.Is<ConsultarBecasActivasQuery>(consulta =>
                consulta.PostulanteEstudianteId == postulanteId
                && consulta.PeriodoAcademicoId == periodoId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_DevolverListaVacia_Cuando_NoHayBecasActivas()
    {
        var consultas = Substitute.For<IQueryDispatcher>();
        consultas
            .ConsultarAsync<
                ConsultarBecasActivasQuery,
                IReadOnlyList<BecaActivaDto>>(
                Arg.Any<ConsultarBecasActivasQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BecaActivaDto>>([]));

        using var fabrica = CrearFabrica(consultas);
        using var cliente = fabrica.CreateClient();
        AutenticarComoServicio(cliente);

        var respuesta = await cliente.GetAsync(
            $"/internal/v1/becas/activas?postulanteId={Guid.NewGuid()}&periodoId={Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuesta.Content.ReadFromJsonAsync<List<BecaActivaDto>>())
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_Responder400_Cuando_FaltaUnFiltroObligatorio()
    {
        var consultas = Substitute.For<IQueryDispatcher>();

        using var fabrica = CrearFabrica(consultas);
        using var cliente = fabrica.CreateClient();
        AutenticarComoServicio(cliente);

        var respuesta = await cliente.GetAsync(
            $"/internal/v1/becas/activas?postulanteId={Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var problema = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        problema.RootElement.GetProperty("code").GetString()
            .Should().Be("SOLICITUD_INVALIDA");

        await consultas.DidNotReceive().ConsultarAsync<
            ConsultarBecasActivasQuery,
            IReadOnlyList<BecaActivaDto>>(
            Arg.Any<ConsultarBecasActivasQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_LaCuentaDeServicioNoTieneElScope()
    {
        var consultas = Substitute.For<IQueryDispatcher>();

        using var fabrica = CrearFabrica(consultas);
        using var cliente = fabrica.CreateClient();
        AutenticarComoServicio(cliente, scope: "openid perfil");

        var respuesta = await cliente.GetAsync(
            $"/internal/v1/becas/activas?postulanteId={Guid.NewGuid()}&periodoId={Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var problema = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        problema.RootElement.GetProperty("code").GetString()
            .Should().Be("PERMISO_DENEGADO");

        await consultas.DidNotReceive().ConsultarAsync<
            ConsultarBecasActivasQuery,
            IReadOnlyList<BecaActivaDto>>(
            Arg.Any<ConsultarBecasActivasQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElClienteNoEstaPermitido()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        AutenticarComoServicio(cliente, clienteId: "cliente-externo");

        var respuesta = await cliente.GetAsync(
            $"/internal/v1/becas/activas?postulanteId={Guid.NewGuid()}&periodoId={Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Debe_Responder401_Cuando_NoHayTokenDeServicio()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(
            $"/internal/v1/becas/activas?postulanteId={Guid.NewGuid()}&periodoId={Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IQueryDispatcher? consultas = null)
        => _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<IQueryDispatcher>();
                servicios.AddSingleton(
                    consultas ?? Substitute.For<IQueryDispatcher>());
            });
        });

    private static void AutenticarComoServicio(
        HttpClient cliente,
        string clienteId = "ms-bec-internal",
        string scope = "becas.internal.read")
    {
        cliente.DefaultRequestHeaders.Add(EncabezadoCliente, clienteId);
        cliente.DefaultRequestHeaders.Add(EncabezadoScopes, scope);
    }
}
