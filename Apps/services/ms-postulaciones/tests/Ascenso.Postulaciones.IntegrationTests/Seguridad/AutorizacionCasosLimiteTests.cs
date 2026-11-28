using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Seguridad;

public sealed class AutorizacionCasosLimiteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public AutorizacionCasosLimiteTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_RegistrarPoliticaResolucionCasosLimiteRankingParaCoordinadorYMiembroComite()
    {
        PoliticasAutorizacionConsolidacion.ResolucionCasosLimiteRanking.Should().Be("ResolucionCasosLimiteRanking");
        PoliticasAutorizacionConsolidacion.RolCoordinadorPrograma.Should().Be("COORDINADOR_PROGRAMA");
        PoliticasAutorizacionConsolidacion.RolMiembroComite.Should().Be("MIEMBRO_COMITE");

        using var fabrica = CrearFabrica();
        using var scope = fabrica.Services.CreateScope();
        var proveedorPoliticas = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
        var servicioAutorizacion = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var politica = await proveedorPoliticas.GetPolicyAsync(
            PoliticasAutorizacionConsolidacion.ResolucionCasosLimiteRanking);
        politica.Should().NotBeNull();

        var coordinador = CrearPrincipal("COORDINADOR_PROGRAMA");
        var miembroComite = CrearPrincipal("MIEMBRO_COMITE");
        var direccion = CrearPrincipal("DIRECCION");
        var adminFinanciero = CrearPrincipal("ADMIN_FINANCIERO");
        var postulante = CrearPrincipal("POSTULANTE");

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politica!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politica!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(direccion, null, politica!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politica!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(postulante, null, politica!)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_EndpointTienePoliticaResolucionCasosLimiteRankingYResultadoEsForbidden()
    {
        var manejador = new ManejadorResultadoAutorizacionRubricas();
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/v1/rankings/convocatorias/casos-limite/resolver";
        contexto.Response.Body = new MemoryStream();
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute(PoliticasAutorizacionConsolidacion.ResolucionCasosLimiteRanking)),
            "EndpointResolucionCasoLimitePrueba");
        contexto.SetEndpoint(endpoint);

        var politicaAutorizacion = new AuthorizationPolicyBuilder()
            .RequireRole(
                PoliticasAutorizacionConsolidacion.RolCoordinadorPrograma,
                PoliticasAutorizacionConsolidacion.RolMiembroComite)
            .Build();
        var resultadoEvaluacion = PolicyAuthorizationResult.Forbid();

        await manejador.HandleAsync(
            _ => Task.CompletedTask,
            contexto,
            politicaAutorizacion,
            resultadoEvaluacion);

        contexto.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        contexto.Response.Body.Seek(0, SeekOrigin.Begin);
        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);
        documento.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status403Forbidden);
        documento.RootElement.GetProperty("codigo").GetString()
            .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
    }

    [Fact]
    public async Task Debe_PermitirACoordinadorProgramaResolverCasoLimiteSinConsultarJefaturaDeComite()
    {
        var (convocatoriaId, postulacionGanadoraId, _, repoJefatura, fabrica) = CrearFabricaConEmpateActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "COORDINADOR_PROGRAMA");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { postulacionGanadoraId },
                justificacion = "Desempate formal autorizado por coordinación de programa."
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        await repoJefatura.DidNotReceive().EsJefaturaActivaEnConvocatoriaAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_AutorizarAMiembroComiteConJefaturaActivaEnResolucionYDenegar403EnConsultaYConfirmacionDeRanking()
    {
        var (convocatoriaId, postulacionGanadoraId, jefaturaId, _, fabrica) = CrearFabricaConEmpateActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, jefaturaId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "MIEMBRO_COMITE");

        var respResolver = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { postulacionGanadoraId },
                justificacion = "Desempate formal autorizado por la jefatura activa del comité evaluador."
            });
        respResolver.StatusCode.Should().Be(HttpStatusCode.OK);

        var respConsultarRanking = await cliente.GetAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}");
        await VerificarPermisoDenegadoAsync(respConsultarRanking);

        var respConfirmarRanking = await cliente.PostAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar",
            content: null);
        await VerificarPermisoDenegadoAsync(respConfirmarRanking);
    }

    [Fact]
    public async Task Debe_Denegar403ConPermisoDenegado_Cuando_MiembroComiteNoEsJefaturaActivaDeLaConvocatoria()
    {
        var (convocatoriaId, postulacionGanadoraId, _, _, fabrica) = CrearFabricaConEmpateActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "MIEMBRO_COMITE");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { postulacionGanadoraId },
                justificacion = "Intento de desempate por miembro regular sin jefatura activa."
            });

        await VerificarPermisoDenegadoAsync(respuesta);
    }

    [Theory]
    [InlineData("DIRECCION")]
    [InlineData("ADMIN_FINANCIERO")]
    public async Task Debe_Denegar403ConPermisoDenegadoEnResolucionDeCasoLimite_Cuando_RolEsDireccionOAdminFinanciero(
        string rol)
    {
        var convocatoriaId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, rol);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { Guid.NewGuid() },
                justificacion = "Justificación formal válida de al menos diez caracteres."
            });

        await VerificarPermisoDenegadoAsync(respuesta);
    }

    [Fact]
    public async Task Debe_Retornar401UnauthorizedEnResolucionDeCasoLimite_Cuando_NoHayToken()
    {
        var convocatoriaId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { Guid.NewGuid() },
                justificacion = "Justificación formal válida de al menos diez caracteres."
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private (
        Guid ConvocatoriaId,
        Guid PostulacionGanadoraId,
        Guid JefaturaUsuarioId,
        IConsultaJefaturaComiteRepository RepoJefatura,
        WebApplicationFactory<Program> Fabrica) CrearFabricaConEmpateActivo()
    {
        var ahora = DateTimeOffset.UtcNow;
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var jefaturaId = Guid.NewGuid();
        var postulacion1Id = Guid.NewGuid();
        var postulacion2Id = Guid.NewGuid();

        var r1 = ResultadoConsolidadoRanking.Crear(
            new ResultadoCalculoConsolidacion(
                convocatoriaId,
                postulacion1Id,
                excepcionEvaluadorUnico: false,
                justificacionEvaluadorUnico: null,
                autorizadorExcepcionId: null,
                cantidadEvaluacionesValidas: 2,
                puntajeConsolidado: 88.00m,
                diferenciaEntreEvaluadores: 4.00m,
                umbralDiscrepanciaAplicado: 20.00m,
                alertaDiscrepancia: false),
            ahora.AddMinutes(-25),
            Guid.NewGuid());

        var r2 = ResultadoConsolidadoRanking.Crear(
            new ResultadoCalculoConsolidacion(
                convocatoriaId,
                postulacion2Id,
                excepcionEvaluadorUnico: false,
                justificacionEvaluadorUnico: null,
                autorizadorExcepcionId: null,
                cantidadEvaluacionesValidas: 2,
                puntajeConsolidado: 88.00m,
                diferenciaEntreEvaluadores: 4.00m,
                umbralDiscrepanciaAplicado: 20.00m,
                alertaDiscrepancia: false),
            ahora.AddMinutes(-20),
            Guid.NewGuid());

        var repoJefatura = Substitute.For<IConsultaJefaturaComiteRepository>();
        repoJefatura.EsJefaturaActivaEnConvocatoriaAsync(
                convocatoriaId,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Guid>(1) == jefaturaId);

        var repoRanking = Substitute.For<IRankingConvocatoriaRepository>();
        repoRanking.ListarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r2]);
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
                [postulacion1Id] = 1500.00m,
                [postulacion2Id] = 1500.00m
            });

        var fabrica = CrearFabrica(
            repoJefatura: repoJefatura,
            repoRanking: repoRanking,
            repoEstadoEvaluaciones: repoEstado,
            convocatoriaClient: convocatoriaClient,
            disponibilidadClient: disponibilidadClient,
            proveedorMonto: proveedorMonto);

        return (convocatoriaId, postulacion2Id, jefaturaId, repoJefatura, fabrica);
    }

    private static async Task VerificarPermisoDenegadoAsync(HttpResponseMessage respuesta)
    {
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        documento.RootElement.GetProperty("status").GetInt32().Should().Be((int)HttpStatusCode.Forbidden);
        documento.RootElement.GetProperty("codigo").GetString()
            .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IConsultaJefaturaComiteRepository? repoJefatura = null,
        IRankingConvocatoriaRepository? repoRanking = null,
        IConsultaEstadoEvaluacionesConvocatoriaRepository? repoEstadoEvaluaciones = null,
        IConvocatoriaEvaluacionClient? convocatoriaClient = null,
        IDisponibilidadRankingClient? disponibilidadClient = null,
        IProveedorMontoEstimadoBeca? proveedorMonto = null)
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
                servicios.RemoveAll<IEntradaListaEsperaRepository>();
                servicios.RemoveAll<IGestorConcurrenciaListaEspera>();
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
                servicios.AddSingleton(repoJefatura ?? Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(Substitute.For<IEntradaListaEsperaRepository>());
                servicios.AddSingleton(Substitute.For<IGestorConcurrenciaListaEspera>());
                servicios.AddSingleton(disponibilidadClient ?? Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(proveedorMonto ?? Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(convocatoriaClient ?? Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
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

    private static ClaimsPrincipal CrearPrincipal(string rol)
    {
        var identidad = new ClaimsIdentity(
            [
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("roles", rol)
            ],
            authenticationType: EsquemaPrueba,
            nameType: "sub",
            roleType: "roles");

        return new ClaimsPrincipal(identidad);
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
