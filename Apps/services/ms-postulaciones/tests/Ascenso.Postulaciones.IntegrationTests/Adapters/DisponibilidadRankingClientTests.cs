using System.Net;
using System.Text;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.IntegrationTests.Adapters;

public sealed class DisponibilidadRankingClientTests
{
    [Fact]
    public async Task Debe_RetornarDisponibilidadProgramaRanking_Cuando_ConvocatoriaYBalanceResponden200()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var urisCapturadas = new List<Uri>();

        var jsonConvocatoria = $$"""
            {
              "id": "{{convocatoriaId}}",
              "programaBecaId": "{{programaId}}",
              "cuposAsignados": 8
            }
            """;

        var jsonBalance = $$"""
            {
              "programaBecaId": "{{programaId}}",
              "moneda": "usd",
              "presupuestoDisponible": 25000.50,
              "cuposDisponibles": 5
            }
            """;

        var cliente = CrearCliente(solicitud =>
        {
            urisCapturadas.Add(solicitud.RequestUri!);
            if (solicitud.RequestUri!.AbsolutePath.Contains("internal/v1/convocatorias", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonConvocatoria, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonBalance, Encoding.UTF8, "application/json")
            };
        });

        var resultado = await cliente.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId);

        urisCapturadas.Select(u => u.ToString()).Should().Equal(
            $"http://ms-prg.local/internal/v1/convocatorias/{convocatoriaId:D}",
            $"http://ms-prg.local/api/v1/programas/{programaId:D}/balance");

        resultado.Should().NotBeNull();
        resultado!.ConvocatoriaId.Should().Be(convocatoriaId);
        resultado.ProgramaBecaId.Should().Be(programaId);
        resultado.CuposAsignadosConvocatoria.Should().Be(8);
        resultado.CuposDisponiblesPrograma.Should().Be(5);
        resultado.CuposDisponiblesEfectivos.Should().Be(5);
        resultado.PresupuestoDisponibleBruto.Should().Be(25000.50m);
        resultado.PresupuestoComprometidoRenovaciones.Should().Be(0.00m);
        resultado.PresupuestoDisponibleEfectivo.Should().Be(25000.50m);
        resultado.Moneda.Should().Be("USD");
    }

    [Fact]
    public async Task Debe_UsarRutaInternaConfigurableYDescontarRenovacionesPendientes_Cuando_SeConfiguraEndpointDeDisponibilidad()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var urisCapturadas = new List<Uri>();

        var jsonConvocatoria = $$"""
            {
              "id": "{{convocatoriaId}}",
              "programaBecaId": "{{programaId}}",
              "cuposAsignados": 3
            }
            """;

        var jsonDisponibilidad = $$"""
            {
              "programaBecaId": "{{programaId}}",
              "moneda": "USD",
              "presupuestoDisponible": 10000.00,
              "comprometidoRenovacionesPendientes": 2500.00,
              "cuposDisponibles": 6
            }
            """;

        var cliente = CrearCliente(
            solicitud =>
            {
                urisCapturadas.Add(solicitud.RequestUri!);
                if (solicitud.RequestUri!.AbsolutePath.Contains("internal/v1/convocatorias", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(jsonConvocatoria, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonDisponibilidad, Encoding.UTF8, "application/json")
                };
            },
            rutaDisponibilidad: "/internal/v1/programas/{programaId}/disponibilidad");

        var resultado = await cliente.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId);

        urisCapturadas.Select(u => u.ToString()).Should().Equal(
            $"http://ms-prg.local/internal/v1/convocatorias/{convocatoriaId:D}",
            $"http://ms-prg.local/internal/v1/programas/{programaId:D}/disponibilidad");

        resultado.Should().NotBeNull();
        resultado!.CuposDisponiblesEfectivos.Should().Be(3);
        resultado.PresupuestoDisponibleBruto.Should().Be(10000.00m);
        resultado.PresupuestoComprometidoRenovaciones.Should().Be(2500.00m);
        resultado.PresupuestoDisponibleEfectivo.Should().Be(7500.00m);
    }

    [Fact]
    public async Task Debe_UsarBaseAddressDeHttpClient_Cuando_UrlBaseEnOpcionesEstaVacia()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var jsonConvocatoria = $$"""
            {
              "id": "{{convocatoriaId}}",
              "programaBecaId": "{{programaId}}",
              "cuposAsignados": 4
            }
            """;
        var jsonBalance = $$"""
            {
              "programaBecaId": "{{programaId}}",
              "moneda": "USD",
              "presupuestoDisponible": 12000.00,
              "cuposDisponibles": 4
            }
            """;

        var cliente = CrearCliente(
            solicitud =>
            {
                var json = solicitud.RequestUri!.AbsolutePath.Contains("convocatorias", StringComparison.Ordinal)
                    ? jsonConvocatoria
                    : jsonBalance;

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            },
            urlBase: string.Empty,
            baseAddress: new Uri("http://ms-prg-fallback.local/"));

        var resultado = await cliente.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId);

        resultado.Should().NotBeNull();
        resultado!.ConvocatoriaId.Should().Be(convocatoriaId);
    }

    [Fact]
    public async Task Debe_RetornarNull_Cuando_ConvocatoriaResponde404()
    {
        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var resultado = await cliente.ObtenerDisponibilidadPorConvocatoriaAsync(Guid.NewGuid());

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Debe_LanzarProgramaBecaNoEncontradoException_Cuando_ProgramaResponde404()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var jsonConvocatoria = $$"""
            {
              "id": "{{convocatoriaId}}",
              "programaBecaId": "{{programaId}}",
              "cuposAsignados": 5
            }
            """;

        var cliente = CrearCliente(solicitud =>
        {
            if (solicitud.RequestUri!.AbsolutePath.Contains("convocatorias", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonConvocatoria, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var accion = () => cliente.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId);

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
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_ConvocatoriaRespondeErrorHttp(HttpStatusCode statusCode)
    {
        var cliente = CrearCliente(_ => new HttpResponseMessage(statusCode));

        var accion = () => cliente.ObtenerDisponibilidadPorConvocatoriaAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_BalanceProgramaRespondeErrorHttp(HttpStatusCode statusCode)
    {
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var jsonConvocatoria = $$"""
            {
              "id": "{{convocatoriaId}}",
              "programaBecaId": "{{programaId}}",
              "cuposAsignados": 5
            }
            """;

        var cliente = CrearCliente(solicitud =>
        {
            if (solicitud.RequestUri!.AbsolutePath.Contains("convocatorias", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonConvocatoria, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(statusCode);
        });

        var accion = () => cliente.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId);

        await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_OcurreTimeoutORedOJsonInvalido()
    {
        var clienteTimeout = CrearCliente(_ => throw new TaskCanceledException("Timeout simulado"));
        var clienteRed = CrearCliente(_ => throw new HttpRequestException("Error de red simulado"));
        var clienteJsonInvalido = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ json-malformado", Encoding.UTF8, "application/json")
        });

        var accionTimeout = () => clienteTimeout.ObtenerDisponibilidadPorConvocatoriaAsync(Guid.NewGuid());
        var accionRed = () => clienteRed.ObtenerDisponibilidadPorConvocatoriaAsync(Guid.NewGuid());
        var accionJson = () => clienteJsonInvalido.ObtenerDisponibilidadPorConvocatoriaAsync(Guid.NewGuid());

        await accionTimeout.Should().ThrowAsync<DependenciaNoDisponibleException>();
        await accionRed.Should().ThrowAsync<DependenciaNoDisponibleException>();
        await accionJson.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_DatosDeConvocatoriaODisponibilidadSonInconsistentes()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();

        var clienteConvocatoriaInconsistente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"id":"{{convocatoriaId}}","programaBecaId":"{{Guid.Empty}}","cuposAsignados":5}""",
                Encoding.UTF8,
                "application/json")
        });

        var clienteBalanceInconsistente = CrearCliente(solicitud =>
        {
            if (solicitud.RequestUri!.AbsolutePath.Contains("convocatorias", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $$"""{"id":"{{convocatoriaId}}","programaBecaId":"{{programaId}}","cuposAsignados":5}""",
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"programaBecaId":"{{programaId}}","moneda":"","presupuestoDisponible":-10,"cuposDisponibles":2}""",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var accionConvocatoria = () => clienteConvocatoriaInconsistente.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId);
        var accionBalance = () => clienteBalanceInconsistente.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId);

        await accionConvocatoria.Should().ThrowAsync<DependenciaNoDisponibleException>();
        await accionBalance.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_PropagarOperationCanceledException_Cuando_TokenDelCallerEsCancelado()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cliente = CrearCliente(_ => throw new OperationCanceledException(cts.Token));

        var accion = () => cliente.ObtenerDisponibilidadPorConvocatoriaAsync(Guid.NewGuid(), cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Debe_LanzarDependenciaNoDisponible_Cuando_UrlBaseORutaNoEstanConfiguradas()
    {
        var clienteSinUrl = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.OK),
            urlBase: string.Empty,
            baseAddress: null);

        var clienteSinRuta = CrearCliente(
            _ => new HttpResponseMessage(HttpStatusCode.OK),
            rutaDisponibilidad: "   ");

        var accionSinUrl = () => clienteSinUrl.ObtenerDisponibilidadPorConvocatoriaAsync(Guid.NewGuid());
        var accionSinRuta = () => clienteSinRuta.ObtenerDisponibilidadPorConvocatoriaAsync(Guid.NewGuid());

        await accionSinUrl.Should().ThrowAsync<DependenciaNoDisponibleException>();
        await accionSinRuta.Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_ArgumentosSonInvalidos()
    {
        var ctorHttpClientNulo = () => new DisponibilidadRankingClient(
            null!,
            Options.Create(new ServiciosExternosOptions()),
            Options.Create(new RankingOptions()),
            NullLogger<DisponibilidadRankingClient>.Instance);

        var ctorServiciosNulo = () => new DisponibilidadRankingClient(
            new HttpClient(),
            null!,
            Options.Create(new RankingOptions()),
            NullLogger<DisponibilidadRankingClient>.Instance);

        var ctorRankingNulo = () => new DisponibilidadRankingClient(
            new HttpClient(),
            Options.Create(new ServiciosExternosOptions()),
            null!,
            NullLogger<DisponibilidadRankingClient>.Instance);

        var ctorLoggerNulo = () => new DisponibilidadRankingClient(
            new HttpClient(),
            Options.Create(new ServiciosExternosOptions()),
            Options.Create(new RankingOptions()),
            null!);

        ctorHttpClientNulo.Should().Throw<ArgumentNullException>();
        ctorServiciosNulo.Should().Throw<ArgumentNullException>();
        ctorRankingNulo.Should().Throw<ArgumentNullException>();
        ctorLoggerNulo.Should().Throw<ArgumentNullException>();

        var cliente = CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var accionVacia = () => cliente.ObtenerDisponibilidadPorConvocatoriaAsync(Guid.Empty);

        await accionVacia.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_RegistrarRepositoriosOpcionesYClientesHttpDeRanking_Cuando_SeConfiguraInfraestructura()
    {
        var servicios = new ServiceCollection();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiciosExternos:Plataforma:UrlBase"] = "http://localhost:5001",
                ["ServiciosExternos:Programas:UrlBase"] = "http://localhost:5002",
                ["ServiciosExternos:TimeoutSegundos"] = "3",
                ["Ranking:RutaDisponibilidadPrograma"] = "internal/v1/programas/{programaId}/disponibilidad",
                ["Ranking:RutaDetallePrograma"] = "api/v1/programas/{programaId}",
                ["Ranking:MontoEstimadoBecaReferenciaPorDefecto"] = "1800.00"
            })
            .Build();

        servicios.AgregarInfraestructuraPostulaciones(
            PostulacionesDbContextFactory.CadenaConexionPorDefecto,
            configuracion);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IRankingConvocatoriaRepository)
            && d.ImplementationType == typeof(RankingConvocatoriaRepository)
            && d.Lifetime == ServiceLifetime.Scoped);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IConsultaEstadoEvaluacionesConvocatoriaRepository)
            && d.ImplementationType == typeof(ConsultaEstadoEvaluacionesConvocatoriaRepository)
            && d.Lifetime == ServiceLifetime.Scoped);

        using var proveedor = servicios.BuildServiceProvider();
        using var scope = proveedor.CreateScope();
        var opciones = scope.ServiceProvider.GetRequiredService<IOptions<RankingOptions>>().Value;
        opciones.RutaDisponibilidadPrograma.Should().Be("internal/v1/programas/{programaId}/disponibilidad");
        opciones.RutaDetallePrograma.Should().Be("api/v1/programas/{programaId}");
        opciones.MontoEstimadoBecaReferenciaPorDefecto.Should().Be(1800.00m);

        scope.ServiceProvider.GetRequiredService<IDisponibilidadRankingClient>()
            .Should().BeOfType<DisponibilidadRankingClient>();
        scope.ServiceProvider.GetRequiredService<IProveedorMontoEstimadoBeca>()
            .Should().BeOfType<ProveedorMontoEstimadoBecaClient>();
    }

    private static DisponibilidadRankingClient CrearCliente(
        Func<HttpRequestMessage, HttpResponseMessage> handler,
        string urlBase = "http://ms-prg.local",
        Uri? baseAddress = null,
        string rutaDisponibilidad = "api/v1/programas/{programaId}/balance")
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
            RutaDisponibilidadPrograma = rutaDisponibilidad
        });

        return new DisponibilidadRankingClient(
            httpClient,
            serviciosOptions,
            rankingOptions,
            NullLogger<DisponibilidadRankingClient>.Instance);
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
