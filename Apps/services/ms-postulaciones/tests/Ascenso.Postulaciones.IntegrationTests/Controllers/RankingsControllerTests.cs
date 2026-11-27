using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Controllers;

public sealed class RankingsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string RolCoordinador = "COORDINADOR_PROGRAMA";
    private const string RolDireccion = "DIRECCION";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public RankingsControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid convocatoriaId,
        decimal puntaje,
        bool alertaDiscrepancia = false,
        bool discrepanciaRevisada = false)
    {
        var ahora = DateTimeOffset.UtcNow;
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId: convocatoriaId,
            postulacionId: Guid.NewGuid(),
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: alertaDiscrepancia ? 25.00m : 5.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: alertaDiscrepancia);

        var resultado = ResultadoConsolidadoRanking.Crear(
            calculo,
            ahora.AddMinutes(-30),
            Guid.NewGuid());

        if (alertaDiscrepancia && discrepanciaRevisada)
        {
            resultado.MarcarDiscrepanciaRevisada(
                "Discrepancia revisada formalmente por coordinación.",
                ahora.AddMinutes(-15),
                Guid.NewGuid());
        }

        return resultado;
    }

    [Fact]
    public async Task Debe_Retornar200OkConRankingProvisionalFiltradoYPaginado_Cuando_SeConsultaPorConvocatoria()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var r1 = CrearResultado(convocatoriaId, 95.00m);
        var r2 = CrearResultado(convocatoriaId, 88.50m);
        var r3 = CrearResultado(convocatoriaId, 82.00m);

        var repoRanking = Substitute.For<IRankingConvocatoriaRepository>();
        repoRanking.ListarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r2, r3]);

        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: 3,
                cuposDisponiblesPrograma: 2,
                presupuestoDisponibleBruto: 10000.00m,
                presupuestoComprometidoRenovaciones: 2000.00m,
                moneda: "USD"));

        var proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
        proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                convocatoriaId,
                programaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [r1.PostulacionId] = 1000.00m,
                [r2.PostulacionId] = 1000.00m,
                [r3.PostulacionId] = 1000.00m
            });

        using var fabrica = CrearFabrica(
            repoRanking: repoRanking,
            disponibilidadClient: disponibilidadClient,
            proveedorMonto: proveedorMonto);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolDireccion);

        var respuesta = await cliente.GetAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}?clasificacion=DENTRO_DE_CUPO&pagina=1&tamano=10");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<RankingConvocatoriaDto>();
        dto.Should().NotBeNull();
        dto!.ConvocatoriaId.Should().Be(convocatoriaId);
        dto.EstadoRanking.Should().Be("PROVISIONAL");
        dto.TotalPostulantes.Should().Be(3);
        dto.TotalDentroDeCupo.Should().Be(2);
        dto.TotalListaDeEspera.Should().Be(1);
        dto.Elementos.Total.Should().Be(2);
        dto.Elementos.Items.Should().HaveCount(2);
        dto.Elementos.Items.Select(e => e.PostulacionId).Should().Equal(r1.PostulacionId, r2.PostulacionId);
        dto.Elementos.Items.All(e => e.Clasificacion == "DENTRO_DE_CUPO").Should().BeTrue();
        dto.Disponibilidad.Should().NotBeNull();
        dto.Disponibilidad!.CuposDisponiblesEfectivos.Should().Be(2);
        dto.Disponibilidad.PresupuestoDisponibleEfectivo.Should().Be(8000.00m);
    }

    [Fact]
    public async Task Debe_Retornar200OkConRankingConfirmado_Cuando_CoordinadorConfirmaRankingValido()
    {
        var ahora = DateTimeOffset.UtcNow;
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();
        var r1 = CrearResultado(convocatoriaId, 95.00m);
        var r2 = CrearResultado(convocatoriaId, 82.00m);

        var repoRanking = Substitute.For<IRankingConvocatoriaRepository>();
        repoRanking.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r2]);

        var repoEstado = Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>();
        repoEstado.ExistenEvaluacionesPendientesOSinConsolidarAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);

        var convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();
        convocatoriaClient.ObtenerVentanaRevisionAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                convocatoriaId,
                EstadoConvocatoriaExterna.Cerrada,
                ahora.AddDays(-10),
                ahora.AddMinutes(-5)));

        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: 1,
                cuposDisponiblesPrograma: 1,
                presupuestoDisponibleBruto: 5000.00m,
                presupuestoComprometidoRenovaciones: 0.00m,
                moneda: "USD"));

        var proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
        proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                convocatoriaId,
                programaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [r1.PostulacionId] = 1500.00m,
                [r2.PostulacionId] = 1500.00m
            });

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(
            repoRanking: repoRanking,
            repoEstadoEvaluaciones: repoEstado,
            convocatoriaClient: convocatoriaClient,
            disponibilidadClient: disponibilidadClient,
            proveedorMonto: proveedorMonto,
            uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar",
            content: null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<RankingConvocatoriaDto>();
        dto.Should().NotBeNull();
        dto!.EstadoRanking.Should().Be("CONFIRMADO");
        dto.ConfirmadoPorUsuarioId.Should().Be(coordinadorId);
        dto.FechaConfirmacionRanking.Should().NotBeNull();
        dto.TotalDentroDeCupo.Should().Be(1);
        dto.TotalListaDeEspera.Should().Be(1);
        dto.Elementos.Items[0].PosicionRanking.Should().Be(1);
        dto.Elementos.Items[0].Clasificacion.Should().Be("DENTRO_DE_CUPO");
        dto.Elementos.Items[1].PosicionRanking.Should().Be(2);
        dto.Elementos.Items[1].Clasificacion.Should().Be("LISTA_DE_ESPERA");

        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_ParametrosDeConsultaOConfirmacionSonInvalidos()
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respGetVacio = await cliente.GetAsync($"/api/v1/rankings/convocatorias/{Guid.Empty}");
        respGetVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respGetClasificacionInvalida = await cliente.GetAsync(
            $"/api/v1/rankings/convocatorias/{Guid.NewGuid()}?clasificacion=INVALIDA");
        respGetClasificacionInvalida.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respGetPaginaCero = await cliente.GetAsync(
            $"/api/v1/rankings/convocatorias/{Guid.NewGuid()}?pagina=0");
        respGetPaginaCero.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respGetTamanoExcesivo = await cliente.GetAsync(
            $"/api/v1/rankings/convocatorias/{Guid.NewGuid()}?tamano=101");
        respGetTamanoExcesivo.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respConfirmarVacio = await cliente.PostAsync(
            $"/api/v1/rankings/convocatorias/{Guid.Empty}/confirmar",
            content: null);
        respConfirmarVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar404409422Y503EnProblemDetails_Cuando_FallanPrecondicionesDeRanking()
    {
        var ahora = DateTimeOffset.UtcNow;
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();

        var repoRanking = Substitute.For<IRankingConvocatoriaRepository>();
        var repoEstado = Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>();
        var convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();
        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        var proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();

        using var fabrica = CrearFabrica(
            repoRanking: repoRanking,
            repoEstadoEvaluaciones: repoEstado,
            convocatoriaClient: convocatoriaClient,
            disponibilidadClient: disponibilidadClient,
            proveedorMonto: proveedorMonto);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        // 1. 404 CONVOCATORIA_NO_ENCONTRADA en GET
        repoRanking.ListarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([]);
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns((DisponibilidadProgramaRanking?)null);

        var respGet404 = await cliente.GetAsync($"/api/v1/rankings/convocatorias/{convocatoriaId}");
        respGet404.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respGet404, ConvocatoriaNoEncontradaException.CodigoError);

        // 2. 422 RANKING_SIN_RESULTADOS en POST confirmar
        repoRanking.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([]);

        var respSinResultados = await cliente.PostAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar",
            content: null);
        respSinResultados.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respSinResultados, RankingSinResultadosConsolidadosException.CodigoError);

        // 3. 409 EVALUACIONES_PENDIENTES
        var r1 = CrearResultado(convocatoriaId, 90.00m, alertaDiscrepancia: true, discrepanciaRevisada: false);
        var r2 = CrearResultado(convocatoriaId, 90.00m, alertaDiscrepancia: false);
        repoRanking.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r2]);
        repoEstado.ExistenEvaluacionesPendientesOSinConsolidarAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(true);

        var respPendientes = await cliente.PostAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar",
            content: null);
        respPendientes.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respPendientes, EvaluacionesPendientesParaConsolidarException.CodigoError);

        // 4. 422 EMPATES_SIN_RESOLVER
        repoEstado.ExistenEvaluacionesPendientesOSinConsolidarAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);
        convocatoriaClient.ObtenerVentanaRevisionAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                convocatoriaId,
                EstadoConvocatoriaExterna.Cerrada,
                ahora.AddDays(-10),
                ahora.AddMinutes(-5)));
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: 1,
                cuposDisponiblesPrograma: 1,
                presupuestoDisponibleBruto: 5000.00m,
                presupuestoComprometidoRenovaciones: 0.00m,
                moneda: "USD"));
        proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                convocatoriaId,
                programaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [r1.PostulacionId] = 1000.00m,
                [r2.PostulacionId] = 1000.00m
            });

        var respEmpate = await cliente.PostAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar",
            content: null);
        respEmpate.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respEmpate, EmpatesSinResolverEnCorteException.CodigoError);

        // 5. 422 DISCREPANCIAS_SIN_REVISAR_EN_CORTE cuando no hay acuse y 200 OK con acuse
        var r3 = CrearResultado(convocatoriaId, 80.00m);
        repoRanking.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r3]);
        proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                convocatoriaId,
                programaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [r1.PostulacionId] = 1000.00m,
                [r3.PostulacionId] = 1000.00m
            });

        var respSinAcuse = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar",
            new { confirmarConAdvertenciaDiscrepancias = false });
        respSinAcuse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respSinAcuse, DiscrepanciasSinRevisarEnCorteException.CodigoError);

        var respConAcuse = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar",
            new { confirmarConAdvertenciaDiscrepancias = true });
        respConAcuse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. 503 DEPENDENCIA_NO_DISPONIBLE
        var otraConvocatoriaId = Guid.NewGuid();
        repoRanking.ListarPorConvocatoriaAsync(otraConvocatoriaId, Arg.Any<CancellationToken>())
            .Returns([]);
        disponibilidadClient
            .When(c => c.ObtenerDisponibilidadPorConvocatoriaAsync(otraConvocatoriaId, Arg.Any<CancellationToken>()))
            .Do(_ => throw new DependenciaNoDisponibleException());

        var resp503 = await cliente.GetAsync($"/api/v1/rankings/convocatorias/{otraConvocatoriaId}");
        resp503.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        await VerificarCodigoErrorAsync(resp503, DependenciaNoDisponibleException.CodigoError);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IRankingConvocatoriaRepository? repoRanking = null,
        IConsultaEstadoEvaluacionesConvocatoriaRepository? repoEstadoEvaluaciones = null,
        IConvocatoriaEvaluacionClient? convocatoriaClient = null,
        IDisponibilidadRankingClient? disponibilidadClient = null,
        IProveedorMontoEstimadoBeca? proveedorMonto = null,
        IUnitOfWork? uow = null)
    {
        return _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<IPostulanteRepository>();
                servicios.RemoveAll<IRubricaRepository>();
                servicios.RemoveAll<IComiteEvaluadorRepository>();
                servicios.RemoveAll<IAsignacionEvaluacionRepository>();
                servicios.RemoveAll<IExcepcionEvaluadorUnicoRepository>();
                servicios.RemoveAll<IEvaluacionRepository>();
                servicios.RemoveAll<IConsultaAsignacionParaEvaluacionRepository>();
                servicios.RemoveAll<IConsultaRubricaParaEvaluacionRepository>();
                servicios.RemoveAll<IConsultaEvaluacionesCoevaluadoresRepository>();
                servicios.RemoveAll<IResultadoConsolidadoRepository>();
                servicios.RemoveAll<IConsultaDatosConsolidacionRepository>();
                servicios.RemoveAll<IProveedorParametrosConsolidacion>();
                servicios.RemoveAll<IRankingConvocatoriaRepository>();
                servicios.RemoveAll<IConsultaEstadoEvaluacionesConvocatoriaRepository>();
                servicios.RemoveAll<IConsultaJefaturaComiteRepository>();
                servicios.RemoveAll<IVerificadorConflictoInteresDecisor>();
                servicios.RemoveAll<IDisponibilidadRankingClient>();
                servicios.RemoveAll<IProveedorMontoEstimadoBeca>();
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IConvocatoriaEvaluacionClient>();
                servicios.RemoveAll<IUnitOfWork>();

                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(repoRanking ?? Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(repoEstadoEvaluaciones ?? Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(disponibilidadClient ?? Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(proveedorMonto ?? Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(convocatoriaClient ?? Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(uow ?? Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);

                servicios.AddAuthentication(opciones =>
                    {
                        opciones.DefaultAuthenticateScheme = EsquemaPrueba;
                        opciones.DefaultChallengeScheme = EsquemaPrueba;
                        opciones.DefaultScheme = EsquemaPrueba;
                    })
                    .AddScheme<AuthenticationSchemeOptions, ManejadorAutenticacionPrueba>(
                        EsquemaPrueba,
                        _ => { });
            });
        });
    }

    private static HttpClient CrearClienteAutenticado(
        WebApplicationFactory<Program> fabrica,
        Guid usuarioId,
        string rol)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, rol);
        return cliente;
    }

    private static async Task VerificarCodigoErrorAsync(HttpResponseMessage respuesta, string codigoEsperado)
    {
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        documento.RootElement.GetProperty("codigo").GetString().Should().Be(codigoEsperado);
    }

    private sealed class ManejadorAutenticacionPrueba : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public ManejadorAutenticacionPrueba(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderSub, out var valoresSub))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new("sub", valoresSub.FirstOrDefault() ?? string.Empty)
            };

            if (Request.Headers.TryGetValue(HeaderRol, out var valoresRol))
            {
                foreach (var rol in valoresRol.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("roles", rol!));
                }
            }

            var identidad = new ClaimsIdentity(claims, EsquemaPrueba, "sub", "roles");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidad), EsquemaPrueba)));
        }
    }
}
