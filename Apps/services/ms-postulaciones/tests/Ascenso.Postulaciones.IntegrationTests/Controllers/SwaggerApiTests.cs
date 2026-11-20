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
}
