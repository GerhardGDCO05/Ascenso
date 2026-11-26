using System.Net;
using System.Text;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.IntegrationTests.Adapters;

public sealed class ProveedorMontoEstimadoBecaClientTests
{
    [Fact]
    public async Task Debe_RetornarDiccionarioVacioSinLlamarHttp_Cuando_ListaDePostulacionesEstaVacia()
    {
        var llamadas = 0;
        var cliente = CrearCliente(_ =>
        {
            llamadas++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var resultado = await cliente.ObtenerMontosEstimadosPorPostulacionAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            []);

        resultado.Should().BeEmpty();
        llamadas.Should().Be(0);
    }

    [Fact]
    public async Task Debe_ProyectarMontoFijoPeriodo_Cuando_PoliticaCoberturaDefineMontoFijoMayorACero()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var postulacion1 = Guid.NewGuid();
        var postulacion2 = Guid.NewGuid();
        Uri? uriCapturada = null;

        var jsonPrograma = $$"""
            {
              "id": "{{programaId}}",
              "politicaCobertura": {
                "modalidad": "MONTO_FIJO",
                "porcentajeCobertura": null,
                "montoFijoPeriodo": 1750.556,
                "moneda": "USD"
              }
            }
            """;

        var cliente = CrearCliente(solicitud =>
        {
            uriCapturada = solicitud.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonPrograma, Encoding.UTF8, "application/json")
            };
        });

        var resultado = await cliente.ObtenerMontosEstimadosPorPostulacionAsync(
            convocatoriaId,
            programaId,
            [postulacion1, postulacion2]);

        uriCapturada.Should().NotBeNull();
        uriCapturada!.ToString().Should().Be($"http://ms-prg.local/api/v1/programas/{programaId:D}");
        resultado.Should().HaveCount(2);
        resultado[postulacion1].Should().Be(1750.56m);
        resultado[postulacion2].Should().Be(1750.56m);
    }

    [Fact]
    public async Task Debe_ProyectarMontoParcialConReferenciaConfigurable_Cuando_PoliticaEsParcialSinMontoFijo()
    {
        var programaId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var jsonPrograma = $$"""
            {
              "id": "{{programaId}}",
              "politicaCobertura": {
                "modalidad": "PARCIAL",
                "porcentajeCobertura": 75.00,
                "montoFijoPeriodo": null,
                "moneda": "USD"
              }
            }
            """;

        var cliente = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonPrograma, Encoding.UTF8, "application/json")
            },
            montoReferencia: 2000.00m);

        var resultado = await cliente.ObtenerMontosEstimadosPorPostulacionAsync(
            Guid.NewGuid(),
            programaId,
            [postulacionId]);

        resultado[postulacionId].Should().Be(1500.00m);
    }

    [Fact]
    public async Task Debe_ProyectarMontoReferenciaTotal_Cuando_PoliticaEsTotalSinMontoFijo()
    {
        var programaId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var jsonPrograma = $$"""
            {
              "id": "{{programaId}}",
              "politicaCobertura": {
                "modalidad": "TOTAL",
                "porcentajeCobertura": 100.00,
                "montoFijoPeriodo": null,
                "moneda": "USD"
              }
            }
            """;

        var cliente = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonPrograma, Encoding.UTF8, "application/json")
            },
            montoReferencia: 1250.00m,
            baseAddress: new Uri("http://ms-prg-fallback.local/"),
            urlBase: string.Empty);

        var resultado = await cliente.ObtenerMontosEstimadosPorPostulacionAsync(
            Guid.NewGuid(),
            programaId,
            [postulacionId]);

        resultado[postulacionId].Should().Be(1250.00m);
    }

    [Fact]
    public async Task Debe_LanzarProgramaBecaNoEncontradoException_Cuando_ProgramaResponde404()
    {
        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var accion = () => cliente.ObtenerMontosEstimadosPorPostulacionAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [Guid.NewGuid()]);

        var ex = await accion.Should().ThrowAsync<ProgramaBecaNoEncontradoException>();
        ex.Which.Codigo.Should().Be(ProgramaBecaNoEncontradoException.CodigoError);
        ex.Which.EstadoHttp.Should().Be(404);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_ProgramaRespondeErrorHttp(HttpStatusCode statusCode)
    {
        var cliente = CrearCliente(_ => new HttpResponseMessage(statusCode));

        var accion = () => cliente.ObtenerMontosEstimadosPorPostulacionAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [Guid.NewGuid()]);

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_OcurreTimeoutRedJsonInvalidoOMontoNoDeterminable()
    {
        var programaId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        var clienteTimeout = CrearCliente(_ => throw new TaskCanceledException("Timeout simulado"));
        var clienteRed = CrearCliente(_ => throw new HttpRequestException("Error de red simulado"));
        var clienteJsonInvalido = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ json-invalido", Encoding.UTF8, "application/json")
        });
        var clienteMontoCero = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{ "id": "{{programaId}}", "politicaCobertura": { "modalidad": "TOTAL", "montoFijoPeriodo": 0 } }""",
                    Encoding.UTF8,
                    "application/json")
            },
            montoReferencia: 0.00m);

        var clienteSinUrl = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.OK),
            urlBase: string.Empty,
            baseAddress: null);

        var accionTimeout = () => clienteTimeout.ObtenerMontosEstimadosPorPostulacionAsync(Guid.NewGuid(), programaId, [postulacionId]);
        var accionRed = () => clienteRed.ObtenerMontosEstimadosPorPostulacionAsync(Guid.NewGuid(), programaId, [postulacionId]);
        var accionJson = () => clienteJsonInvalido.ObtenerMontosEstimadosPorPostulacionAsync(Guid.NewGuid(), programaId, [postulacionId]);
        var accionMontoCero = () => clienteMontoCero.ObtenerMontosEstimadosPorPostulacionAsync(Guid.NewGuid(), programaId, [postulacionId]);
        var accionSinUrl = () => clienteSinUrl.ObtenerMontosEstimadosPorPostulacionAsync(Guid.NewGuid(), programaId, [postulacionId]);

        await accionTimeout.Should().ThrowAsync<DependenciaNoDisponibleException>();
        await accionRed.Should().ThrowAsync<DependenciaNoDisponibleException>();
        await accionJson.Should().ThrowAsync<DependenciaNoDisponibleException>();
        await accionMontoCero.Should().ThrowAsync<DependenciaNoDisponibleException>();
        await accionSinUrl.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_PropagarOperationCanceledException_Cuando_TokenDelCallerEsCancelado()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cliente = CrearCliente(_ => throw new OperationCanceledException(cts.Token));

        var accion = () => cliente.ObtenerMontosEstimadosPorPostulacionAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [Guid.NewGuid()],
            cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_ArgumentosSonInvalidos()
    {
        var ctorHttpClientNulo = () => new ProveedorMontoEstimadoBecaClient(
            null!,
            Options.Create(new ServiciosExternosOptions()),
            Options.Create(new RankingOptions()),
            NullLogger<ProveedorMontoEstimadoBecaClient>.Instance);

        var ctorServiciosNulo = () => new ProveedorMontoEstimadoBecaClient(
            new HttpClient(),
            null!,
            Options.Create(new RankingOptions()),
            NullLogger<ProveedorMontoEstimadoBecaClient>.Instance);

        var ctorRankingNulo = () => new ProveedorMontoEstimadoBecaClient(
            new HttpClient(),
            Options.Create(new ServiciosExternosOptions()),
            null!,
            NullLogger<ProveedorMontoEstimadoBecaClient>.Instance);

        var ctorLoggerNulo = () => new ProveedorMontoEstimadoBecaClient(
            new HttpClient(),
            Options.Create(new ServiciosExternosOptions()),
            Options.Create(new RankingOptions()),
            null!);

        ctorHttpClientNulo.Should().Throw<ArgumentNullException>();
        ctorServiciosNulo.Should().Throw<ArgumentNullException>();
        ctorRankingNulo.Should().Throw<ArgumentNullException>();
        ctorLoggerNulo.Should().Throw<ArgumentNullException>();

        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var convocatoriaVacia = () => cliente.ObtenerMontosEstimadosPorPostulacionAsync(Guid.Empty, Guid.NewGuid(), [Guid.NewGuid()]);
        var programaVacio = () => cliente.ObtenerMontosEstimadosPorPostulacionAsync(Guid.NewGuid(), Guid.Empty, [Guid.NewGuid()]);
        var listaNula = () => cliente.ObtenerMontosEstimadosPorPostulacionAsync(Guid.NewGuid(), Guid.NewGuid(), null!);
        var postulacionVacia = () => cliente.ObtenerMontosEstimadosPorPostulacionAsync(Guid.NewGuid(), Guid.NewGuid(), [Guid.Empty]);

        await convocatoriaVacia.Should().ThrowAsync<ArgumentException>();
        await programaVacio.Should().ThrowAsync<ArgumentException>();
        await listaNula.Should().ThrowAsync<ArgumentNullException>();
        await postulacionVacia.Should().ThrowAsync<ArgumentException>();
    }

    private static ProveedorMontoEstimadoBecaClient CrearCliente(
        Func<HttpRequestMessage, HttpResponseMessage> handler,
        string urlBase = "http://ms-prg.local",
        Uri? baseAddress = null,
        decimal montoReferencia = 1000.00m,
        string rutaDetallePrograma = "api/v1/programas/{programaId}")
    {
        var httpClient = new HttpClient(new DelegatingHandlerFalso(handler))
        {
            BaseAddress = baseAddress
        };
        var serviciosOptions = Options.Create(new ServiciosExternosOptions
        {
            Programas = new ServicioExternoEndpointOptions { UrlBase = urlBase },
            TimeoutSegundos = 3
        });
        var rankingOptions = Options.Create(new RankingOptions
        {
            RutaDetallePrograma = rutaDetallePrograma,
            MontoEstimadoBecaReferenciaPorDefecto = montoReferencia
        });

        return new ProveedorMontoEstimadoBecaClient(
            httpClient,
            serviciosOptions,
            rankingOptions,
            NullLogger<ProveedorMontoEstimadoBecaClient>.Instance);
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
