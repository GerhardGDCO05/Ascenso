using System.Net;
using System.Text;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.IntegrationTests.Adapters;

public sealed class ConvocatoriaEvaluacionClientTests
{
    private static readonly DateTimeOffset _inicioEvaluacion =
        new(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset _cierreEvaluacion =
        new(2026, 11, 30, 23, 59, 59, TimeSpan.Zero);

    [Theory]
    [InlineData("PUBLICADA", EstadoConvocatoriaExterna.Publicada)]
    [InlineData("CERRADA", EstadoConvocatoriaExterna.Cerrada)]
    [InlineData("BORRADOR", EstadoConvocatoriaExterna.Borrador)]
    [InlineData("CANCELADA", EstadoConvocatoriaExterna.Cancelada)]
    public async Task Debe_RetornarVentanaRevision_Cuando_Respuesta200EsValida(
        string codigoEstado,
        EstadoConvocatoriaExterna estadoEsperado)
    {
        var convocatoriaId = Guid.NewGuid();
        Uri? uriCapturada = null;
        var json = $$"""
            {
              "id": "{{convocatoriaId}}",
              "programaBecaId": "{{Guid.NewGuid()}}",
              "estado": "{{codigoEstado}}",
              "fechaInicioEvaluacion": "{{_inicioEvaluacion:O}}",
              "fechaCierreEvaluacion": "{{_cierreEvaluacion:O}}"
            }
            """;

        var cliente = CrearCliente(solicitud =>
        {
            uriCapturada = solicitud.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var resultado = await cliente.ObtenerVentanaRevisionAsync(convocatoriaId);

        uriCapturada.Should().NotBeNull();
        uriCapturada!.ToString().Should().Be($"http://ms-prg.local/internal/v1/convocatorias/{convocatoriaId:D}");
        resultado.Should().NotBeNull();
        resultado!.ConvocatoriaId.Should().Be(convocatoriaId);
        resultado.EstadoConvocatoria.Should().Be(estadoEsperado);
        resultado.FechaInicioEvaluacion.Should().Be(_inicioEvaluacion);
        resultado.FechaCierreEvaluacion.Should().Be(_cierreEvaluacion);
    }

    [Fact]
    public async Task Debe_UsarBaseAddressDeHttpClient_Cuando_UrlBaseEnOpcionesEstaVacia()
    {
        var convocatoriaId = Guid.NewGuid();
        var json = $$"""
            {
              "id": "{{convocatoriaId}}",
              "estado": "PUBLICADA",
              "fechaInicioEvaluacion": "{{_inicioEvaluacion:O}}",
              "fechaCierreEvaluacion": "{{_cierreEvaluacion:O}}"
            }
            """;

        var cliente = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            },
            urlBase: string.Empty,
            baseAddress: new Uri("http://ms-prg-fallback.local/"));

        var resultado = await cliente.ObtenerVentanaRevisionAsync(convocatoriaId);

        resultado.Should().NotBeNull();
        resultado!.ConvocatoriaId.Should().Be(convocatoriaId);
    }

    [Fact]
    public async Task Debe_RetornarNull_Cuando_MsPrgResponde404()
    {
        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var resultado = await cliente.ObtenerVentanaRevisionAsync(Guid.NewGuid());

        resultado.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_MsPrgResponde401O403O5xx(
        HttpStatusCode statusCode)
    {
        var cliente = CrearCliente(_ => new HttpResponseMessage(statusCode));

        var accion = () => cliente.ObtenerVentanaRevisionAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_OcurreTimeout()
    {
        var cliente = CrearCliente(_ => throw new TaskCanceledException("Timeout simulado"));

        var accion = () => cliente.ObtenerVentanaRevisionAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_PropagarOperationCanceledException_Cuando_TokenDelCallerEsCancelado()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cliente = CrearCliente(_ => throw new OperationCanceledException(cts.Token));

        var accion = () => cliente.ObtenerVentanaRevisionAsync(Guid.NewGuid(), cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_OcurreHttpRequestException()
    {
        var cliente = CrearCliente(_ => throw new HttpRequestException("Fallo de red simulado"));

        var accion = () => cliente.ObtenerVentanaRevisionAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_JsonEsMalformado()
    {
        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ json-invalido", Encoding.UTF8, "application/json")
        });

        var accion = () => cliente.ObtenerVentanaRevisionAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_IdEnRespuestaEsVacio()
    {
        var json = $$"""
            {
              "id": "{{Guid.Empty}}",
              "estado": "PUBLICADA",
              "fechaInicioEvaluacion": "{{_inicioEvaluacion:O}}",
              "fechaCierreEvaluacion": "{{_cierreEvaluacion:O}}"
            }
            """;

        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var accion = () => cliente.ObtenerVentanaRevisionAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_IdEnRespuestaNoCoincideConSolicitado()
    {
        var json = $$"""
            {
              "id": "{{Guid.NewGuid()}}",
              "estado": "PUBLICADA",
              "fechaInicioEvaluacion": "{{_inicioEvaluacion:O}}",
              "fechaCierreEvaluacion": "{{_cierreEvaluacion:O}}"
            }
            """;

        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var accion = () => cliente.ObtenerVentanaRevisionAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ESTADO_DESCONOCIDO")]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_EstadoEnRespuestaEsInvalidoODesconocido(
        string estadoInvalido)
    {
        var convocatoriaId = Guid.NewGuid();
        var json = $$"""
            {
              "id": "{{convocatoriaId}}",
              "estado": "{{estadoInvalido}}",
              "fechaInicioEvaluacion": "{{_inicioEvaluacion:O}}",
              "fechaCierreEvaluacion": "{{_cierreEvaluacion:O}}"
            }
            """;

        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var accion = () => cliente.ObtenerVentanaRevisionAsync(convocatoriaId);

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_FechasEvaluacionEstanInvertidas()
    {
        var convocatoriaId = Guid.NewGuid();
        var json = $$"""
            {
              "id": "{{convocatoriaId}}",
              "estado": "PUBLICADA",
              "fechaInicioEvaluacion": "{{_cierreEvaluacion:O}}",
              "fechaCierreEvaluacion": "{{_inicioEvaluacion:O}}"
            }
            """;

        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var accion = () => cliente.ObtenerVentanaRevisionAsync(convocatoriaId);

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_UrlBaseNoEstaConfigurada()
    {
        var cliente = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.OK),
            urlBase: string.Empty,
            baseAddress: null);

        var accion = () => cliente.ObtenerVentanaRevisionAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarArgumentException_Cuando_ConvocatoriaIdEsVacio()
    {
        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var accion = () => cliente.ObtenerVentanaRevisionAsync(Guid.Empty);

        await accion.Should().ThrowAsync<ArgumentException>();
    }

    private static ConvocatoriaEvaluacionClient CrearCliente(
        Func<HttpRequestMessage, HttpResponseMessage> handler,
        string urlBase = "http://ms-prg.local",
        Uri? baseAddress = null)
    {
        var httpClient = new HttpClient(new DelegatingHandlerFalso(handler))
        {
            BaseAddress = baseAddress
        };
        var opciones = Options.Create(new ServiciosExternosOptions
        {
            Programas = new ServicioExternoEndpointOptions { UrlBase = urlBase },
            TimeoutSegundos = 3
        });

        return new ConvocatoriaEvaluacionClient(
            httpClient,
            opciones,
            NullLogger<ConvocatoriaEvaluacionClient>.Instance);
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
