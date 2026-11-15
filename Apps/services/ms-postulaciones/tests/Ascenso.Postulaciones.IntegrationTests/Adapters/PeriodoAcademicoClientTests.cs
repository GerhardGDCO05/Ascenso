using System.Net;
using System.Text;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.IntegrationTests.Adapters;

public sealed class PeriodoAcademicoClientTests
{
    [Fact]
    public async Task Debe_RetornarPeriodoActual_Cuando_RespuestaEsValida()
    {
        var periodoId = Guid.NewGuid();
        var json = $$"""{"id":"{{periodoId}}","nombre":"2026-II","estado":"VIGENTE"}""";
        var cliente = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            },
            urlBase: "http://ms-plt.local",
            esDevelopment: false);

        var resultado = await cliente.ObtenerPeriodoActualAsync();

        resultado.Id.Should().Be(periodoId);
        resultado.Nombre.Should().Be("2026-II");
        resultado.Estado.Should().Be(EstadoPeriodoAcademico.Vigente);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_RespuestaEsErrorYSinFallback(HttpStatusCode statusCode)
    {
        var cliente = CrearCliente(
            _ => new HttpResponseMessage(statusCode),
            urlBase: "http://ms-plt.local",
            esDevelopment: false);

        var accion = () => cliente.ObtenerPeriodoActualAsync();

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_OcurreTimeout()
    {
        var cliente = CrearCliente(
            _ => throw new TaskCanceledException("Timeout simulado"),
            urlBase: "http://ms-plt.local",
            esDevelopment: false);

        var accion = () => cliente.ObtenerPeriodoActualAsync();

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_UsarFallback_Cuando_EsDevelopmentYFallaDependencia()
    {
        var fallbackId = Guid.NewGuid();
        var cliente = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            urlBase: "http://ms-plt.local",
            esDevelopment: true,
            fallbackId: fallbackId.ToString());

        var resultado = await cliente.ObtenerPeriodoActualAsync();

        resultado.Id.Should().Be(fallbackId);
        resultado.Estado.Should().Be(EstadoPeriodoAcademico.Vigente);
    }

    [Fact]
    public async Task Debe_IgnorarFallback_Cuando_NoEsDevelopment()
    {
        var cliente = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            urlBase: "http://ms-plt.local",
            esDevelopment: false,
            fallbackId: Guid.NewGuid().ToString());

        var accion = () => cliente.ObtenerPeriodoActualAsync();

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    private static PeriodoAcademicoClient CrearCliente(
        Func<HttpRequestMessage, HttpResponseMessage> handler,
        string urlBase,
        bool esDevelopment,
        string fallbackId = "")
    {
        var httpClient = new HttpClient(new DelegatingHandlerFalso(handler));
        var serviciosOptions = Options.Create(new ServiciosExternosOptions
        {
            Plataforma = new ServicioExternoEndpointOptions { UrlBase = urlBase },
            TimeoutSegundos = 3
        });
        var periodoOptions = Options.Create(new PeriodoActualOptions { FallbackId = fallbackId });
        var entorno = new EntornoPrueba(esDevelopment ? Environments.Development : Environments.Production);

        return new PeriodoAcademicoClient(
            httpClient,
            serviciosOptions,
            periodoOptions,
            entorno,
            NullLogger<PeriodoAcademicoClient>.Instance);
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

    private sealed class EntornoPrueba : IHostEnvironment
    {
        public EntornoPrueba(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
