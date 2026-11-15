using System.Net;
using System.Text;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.IntegrationTests.Adapters;

public sealed class CatalogoAcademicoClientTests
{
    [Fact]
    public async Task Debe_RetornarUniversidadYCarreras_Cuando_RespuestaEsValida()
    {
        var universidadId = Guid.NewGuid();
        var carreraId = Guid.NewGuid();
        var json = $$"""
            {
              "id": "{{universidadId}}",
              "nombre": "Universidad Central",
              "estado": "ACTIVA",
              "tieneConvenioVigente": true,
              "carreras": [
                { "id": "{{carreraId}}", "nombre": "Ingeniería", "activa": true }
              ]
            }
            """;

        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var resultado = await cliente.ObtenerUniversidadAsync(universidadId);

        resultado.Id.Should().Be(universidadId);
        resultado.Estado.Should().Be(EstadoUniversidadAliada.Activa);
        resultado.TieneConvenioVigente.Should().BeTrue();
        resultado.Carreras.Should().ContainSingle(c => c.Id == carreraId && c.Activa);
    }

    [Fact]
    public async Task Debe_LanzarUniversidadNoElegible_Cuando_CatalogoResponde404()
    {
        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var accion = () => cliente.ObtenerUniversidadAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<UniversidadNoElegibleException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_CatalogoResponde5xx(HttpStatusCode statusCode)
    {
        var cliente = CrearCliente(_ => new HttpResponseMessage(statusCode));

        var accion = () => cliente.ObtenerUniversidadAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_OcurreTimeout()
    {
        var cliente = CrearCliente(_ => throw new TaskCanceledException("Timeout simulado"));

        var accion = () => cliente.ObtenerUniversidadAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    private static CatalogoAcademicoClient CrearCliente(
        Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClient = new HttpClient(new DelegatingHandlerFalso(handler));
        var opciones = Options.Create(new ServiciosExternosOptions
        {
            Programas = new ServicioExternoEndpointOptions { UrlBase = "http://ms-prg.local" },
            TimeoutSegundos = 3
        });

        return new CatalogoAcademicoClient(
            httpClient,
            opciones,
            NullLogger<CatalogoAcademicoClient>.Instance);
    }

    private sealed class DelegatingHandlerFalso : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public DelegatingHandlerFalso(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
