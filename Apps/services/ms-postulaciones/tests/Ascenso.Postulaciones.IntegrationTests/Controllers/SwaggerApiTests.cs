using System.Net;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Controllers;

public sealed class SwaggerApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _fabricaBase;

    public SwaggerApiTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_DocumentarPutSocioeconomicoYSusEsquemas_Cuando_EntornoEsDevelopment()
    {
        using var fabrica = _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.UseEnvironment("Development");
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton(Substitute.For<IUsuarioActual>());
                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);
            });
        });
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;

        var ruta = raiz.GetProperty("paths")
            .GetProperty("/api/v1/postulantes/mio/socioeconomico");
        var operacionPut = ruta.GetProperty("put");
        var respuestas = operacionPut.GetProperty("responses");

        foreach (var codigo in new[] { "200", "400", "401", "404", "409", "422" })
        {
            respuestas.TryGetProperty(codigo, out _).Should().BeTrue();
        }

        var esquemaRespuesta = respuestas.GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        esquemaRespuesta.Should().Contain("PostulanteDto");

        var esquemaSolicitud = operacionPut.GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        esquemaSolicitud.Should().Contain("ActualizarPerfilSocioeconomicoRequest");

        var esquemas = raiz.GetProperty("components").GetProperty("schemas");
        esquemas.TryGetProperty("ActualizarPerfilSocioeconomicoRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("DatosMiembroFamiliarRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("PostulanteDto", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Debe_DocumentarEndpointsDeRubricasYSusEsquemas_Cuando_EntornoEsDevelopment()
    {
        using var fabrica = _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.UseEnvironment("Development");
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton(Substitute.For<IUsuarioActual>());
                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);
            });
        });
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;
        var rutas = raiz.GetProperty("paths");

        VerificarRespuestas(rutas.GetProperty("/api/v1/rubricas").GetProperty("post"), "201", ["201", "400", "401", "403", "409"]);
        VerificarRespuestas(rutas.GetProperty("/api/v1/rubricas/{id}").GetProperty("get"), "200", ["200", "401", "403", "404"]);
        VerificarRespuestas(rutas.GetProperty("/api/v1/rubricas/{id}/criterios").GetProperty("post"), "201", ["201", "400", "401", "403", "404", "409"]);

        var rutaCriterio = rutas.GetProperty("/api/v1/rubricas/{id}/criterios/{criterioId}");
        VerificarRespuestas(rutaCriterio.GetProperty("put"), "200", ["200", "400", "401", "403", "404", "409"]);
        VerificarRespuestas(rutaCriterio.GetProperty("delete"), "200", ["200", "401", "403", "404", "409"]);

        VerificarRespuestas(rutas.GetProperty("/api/v1/rubricas/{id}/publicar").GetProperty("post"), "200", ["200", "401", "403", "404", "409", "422"]);
        rutas.TryGetProperty("/api/v1/rubricas/{id}/bloquear", out _).Should().BeFalse();

        var esquemas = raiz.GetProperty("components").GetProperty("schemas");
        esquemas.TryGetProperty("CrearRubricaRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("GuardarCriterioRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("RubricaDto", out var esquemaRubricaDto).Should().BeTrue();
        esquemas.TryGetProperty("CriterioEvaluacionDto", out _).Should().BeTrue();

        var propiedadesRubricaDto = esquemaRubricaDto.GetProperty("properties");
        propiedadesRubricaDto.TryGetProperty("motivoBloqueo", out _).Should().BeTrue();
        propiedadesRubricaDto.TryGetProperty("fechaBloqueo", out _).Should().BeTrue();
        propiedadesRubricaDto.TryGetProperty("soloLectura", out _).Should().BeTrue();
    }

    private static void VerificarRespuestas(
        JsonElement operacion,
        string codigoExito,
        string[] codigosEsperados)
    {
        var respuestas = operacion.GetProperty("responses");
        foreach (var codigo in codigosEsperados)
        {
            respuestas.TryGetProperty(codigo, out _).Should().BeTrue();
        }

        var esquemaRespuesta = respuestas.GetProperty(codigoExito)
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        esquemaRespuesta.Should().Contain("RubricaDto");
    }
}
