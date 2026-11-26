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

public sealed class AutorizacionRankingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string HeaderScope = "X-Test-Scope";
    private const string HeaderScp = "X-Test-Scp";
    private const string HeaderPermiso = "X-Test-Permiso";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public AutorizacionRankingTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_RegistrarPoliticaConsultaAdjudicabilidadInternaConRolesScopesYPermisosDefinidos()
    {
        PoliticasAutorizacionConsolidacion.ConsultaAdjudicabilidadInterna.Should().Be("ConsultaAdjudicabilidadInterna");
        PoliticasAutorizacionConsolidacion.ScopeLecturaInterna.Should().Be("postulaciones.internal.read");
        PoliticasAutorizacionConsolidacion.PermisoLecturaInterna.Should().Be("postulaciones:leer");

        using var fabrica = CrearFabrica();
        using var scope = fabrica.Services.CreateScope();
        var proveedorPoliticas = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
        var servicioAutorizacion = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var politicaAdjudicabilidad = await proveedorPoliticas.GetPolicyAsync(
            PoliticasAutorizacionConsolidacion.ConsultaAdjudicabilidadInterna);
        politicaAdjudicabilidad.Should().NotBeNull();

        var coordinador = CrearPrincipal(rol: "COORDINADOR_PROGRAMA");
        var direccion = CrearPrincipal(rol: "DIRECCION");
        var servicioScope = CrearPrincipal(scope: "openid postulaciones.internal.read profile");
        var servicioScp = CrearPrincipal(scp: "postulaciones.internal.read");
        var servicioPermiso = CrearPrincipal(permiso: "postulaciones:leer");
        var miembroComite = CrearPrincipal(rol: "MIEMBRO_COMITE");
        var adminFinanciero = CrearPrincipal(rol: "ADMIN_FINANCIERO");
        var servicioOtroScope = CrearPrincipal(scope: "postulaciones.internal.write");

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaAdjudicabilidad!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(direccion, null, politicaAdjudicabilidad!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(servicioScope, null, politicaAdjudicabilidad!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(servicioScp, null, politicaAdjudicabilidad!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(servicioPermiso, null, politicaAdjudicabilidad!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaAdjudicabilidad!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaAdjudicabilidad!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(servicioOtroScope, null, politicaAdjudicabilidad!)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_EndpointTienePoliticaConsultaAdjudicabilidadInternaYResultadoEsForbidden()
    {
        var manejador = new ManejadorResultadoAutorizacionRubricas();
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/internal/v1/postulaciones/adjudicable";
        contexto.Response.Body = new MemoryStream();
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute(PoliticasAutorizacionConsolidacion.ConsultaAdjudicabilidadInterna)),
            "EndpointAdjudicabilidadPrueba");
        contexto.SetEndpoint(endpoint);

        var politicaAutorizacion = new AuthorizationPolicyBuilder()
            .RequireRole(PoliticasAutorizacionConsolidacion.RolCoordinadorPrograma)
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
    public async Task Debe_PermitirAccesoACoordinadorProgramaEnLosTresEndpointsDeRankingYAdjudicabilidad()
    {
        var (convocatoriaId, postulacionId, fabrica) = CrearFabricaConEscenarioActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "COORDINADOR_PROGRAMA");

        var respObtenerRanking = await cliente.GetAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}");
        respObtenerRanking.StatusCode.Should().Be(HttpStatusCode.OK);

        var respConfirmarRanking = await cliente.PostAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar",
            content: null);
        respConfirmarRanking.StatusCode.Should().Be(HttpStatusCode.OK);

        var respAdjudicabilidad = await cliente.GetAsync(
            $"/internal/v1/postulaciones/{postulacionId}/adjudicable");
        respAdjudicabilidad.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Debe_PermitirADireccionEnConsultaRankingYAdjudicabilidadYDenegar403EnConfirmarRanking()
    {
        var (convocatoriaId, postulacionId, fabrica) = CrearFabricaConEscenarioActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "DIRECCION");

        var respObtenerRanking = await cliente.GetAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}");
        respObtenerRanking.StatusCode.Should().Be(HttpStatusCode.OK);

        var respAdjudicabilidad = await cliente.GetAsync(
            $"/internal/v1/postulaciones/{postulacionId}/adjudicable");
        respAdjudicabilidad.StatusCode.Should().Be(HttpStatusCode.OK);

        var respConfirmarRanking = await cliente.PostAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar",
            content: null);
        await VerificarPermisoDenegadoAsync(respConfirmarRanking);
    }

    [Theory]
    [InlineData("scope", "postulaciones.internal.read")]
    [InlineData("scp", "postulaciones.internal.read")]
    [InlineData("permissions", "postulaciones:leer")]
    public async Task Debe_AutorizarTokenDeServicioEnAdjudicabilidadInternaYDenegar403EnEndpointsDeRankings(
        string tipoCredencial,
        string valorCredencial)
    {
        var (convocatoriaId, postulacionId, fabrica) = CrearFabricaConEscenarioActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());

        switch (tipoCredencial)
        {
            case "scope":
                cliente.DefaultRequestHeaders.Add(HeaderScope, valorCredencial);
                break;
            case "scp":
                cliente.DefaultRequestHeaders.Add(HeaderScp, valorCredencial);
                break;
            case "permissions":
                cliente.DefaultRequestHeaders.Add(HeaderPermiso, valorCredencial);
                break;
        }

        var respAdjudicabilidad = await cliente.GetAsync(
            $"/internal/v1/postulaciones/{postulacionId}/adjudicable");
        respAdjudicabilidad.StatusCode.Should().Be(HttpStatusCode.OK);

        var respObtenerRanking = await cliente.GetAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}");
        await VerificarPermisoDenegadoAsync(respObtenerRanking);

        var respConfirmarRanking = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar",
            new { confirmarConAdvertenciaDiscrepancias = false });
        await VerificarPermisoDenegadoAsync(respConfirmarRanking);
    }

    [Theory]
    [InlineData("MIEMBRO_COMITE")]
    [InlineData("ADMIN_FINANCIERO")]
    public async Task Debe_Denegar403ConPermisoDenegadoEnLosTresEndpoints_Cuando_RolEsMiembroComiteOAdminFinanciero(
        string rol)
    {
        var convocatoriaId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, rol);

        var respuestas = new[]
        {
            await cliente.GetAsync($"/api/v1/rankings/convocatorias/{convocatoriaId}"),
            await cliente.PostAsync($"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar", content: null),
            await cliente.GetAsync($"/internal/v1/postulaciones/{postulacionId}/adjudicable")
        };

        foreach (var respuesta in respuestas)
        {
            await VerificarPermisoDenegadoAsync(respuesta);
        }
    }

    [Fact]
    public async Task Debe_Retornar401UnauthorizedEnLosTresEndpoints_Cuando_NoHayToken()
    {
        var convocatoriaId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuestas = new[]
        {
            await cliente.GetAsync($"/api/v1/rankings/convocatorias/{convocatoriaId}"),
            await cliente.PostAsync($"/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar", content: null),
            await cliente.GetAsync($"/internal/v1/postulaciones/{postulacionId}/adjudicable")
        };

        foreach (var respuesta in respuestas)
        {
            respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    private (Guid ConvocatoriaId, Guid PostulacionId, WebApplicationFactory<Program> Fabrica) CrearFabricaConEscenarioActivo()
    {
        var ahora = DateTimeOffset.UtcNow;
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        var resultado = ResultadoConsolidadoRanking.Crear(
            new ResultadoCalculoConsolidacion(
                convocatoriaId,
                postulacionId,
                excepcionEvaluadorUnico: false,
                justificacionEvaluadorUnico: null,
                autorizadorExcepcionId: null,
                cantidadEvaluacionesValidas: 2,
                puntajeConsolidado: 91.00m,
                diferenciaEntreEvaluadores: 4.00m,
                umbralDiscrepanciaAplicado: 20.00m,
                alertaDiscrepancia: false),
            ahora.AddMinutes(-20),
            Guid.NewGuid());

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ObtenerPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(resultado);

        var repoRanking = Substitute.For<IRankingConvocatoriaRepository>();
        repoRanking.ListarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([resultado]);
        repoRanking.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([resultado]);

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
                cuposAsignadosConvocatoria: 2,
                cuposDisponiblesPrograma: 2,
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
                [postulacionId] = 1500.00m
            });

        var fabrica = CrearFabrica(
            repoResultados: repoResultados,
            repoRanking: repoRanking,
            repoEstadoEvaluaciones: repoEstado,
            convocatoriaClient: convocatoriaClient,
            disponibilidadClient: disponibilidadClient,
            proveedorMonto: proveedorMonto);

        return (convocatoriaId, postulacionId, fabrica);
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
        IResultadoConsolidadoRepository? repoResultados = null,
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
                servicios.AddSingleton(repoResultados ?? Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(repoRanking ?? Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(repoEstadoEvaluaciones ?? Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
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

    private static ClaimsPrincipal CrearPrincipal(
        string? rol = null,
        string? scope = null,
        string? scp = null,
        string? permiso = null)
    {
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(rol))
        {
            claims.Add(new Claim("roles", rol));
        }

        if (!string.IsNullOrWhiteSpace(scope))
        {
            claims.Add(new Claim("scope", scope));
        }

        if (!string.IsNullOrWhiteSpace(scp))
        {
            claims.Add(new Claim("scp", scp));
        }

        if (!string.IsNullOrWhiteSpace(permiso))
        {
            claims.Add(new Claim("permissions", permiso));
        }

        var identidad = new ClaimsIdentity(
            claims,
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

            if (Request.Headers.TryGetValue(HeaderScope, out var valoresScope))
            {
                foreach (var scope in valoresScope.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("scope", scope!));
                }
            }

            if (Request.Headers.TryGetValue(HeaderScp, out var valoresScp))
            {
                foreach (var scp in valoresScp.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("scp", scp!));
                }
            }

            if (Request.Headers.TryGetValue(HeaderPermiso, out var valoresPermiso))
            {
                foreach (var permiso in valoresPermiso.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("permissions", permiso!));
                }
            }

            var identidad = new ClaimsIdentity(claims, EsquemaPrueba, "sub", "roles");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidad), EsquemaPrueba)));
        }
    }
}
