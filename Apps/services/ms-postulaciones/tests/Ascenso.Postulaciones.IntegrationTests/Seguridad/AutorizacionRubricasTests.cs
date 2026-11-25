using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
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

public sealed class AutorizacionRubricasTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public AutorizacionRubricasTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_RegistrarPoliticasDeRubricasConPermisoDeLecturaParaMiembroComiteYEscrituraSoloParaCoordinador()
    {
        PoliticasAutorizacionRubricas.RolCoordinadorPrograma.Should().Be("COORDINADOR_PROGRAMA");
        PoliticasAutorizacionRubricas.RolMiembroComite.Should().Be("MIEMBRO_COMITE");

        using var fabrica = CrearFabrica();
        using var scope = fabrica.Services.CreateScope();
        var proveedorPoliticas = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
        var servicioAutorizacion = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var politicaCoordinador = await proveedorPoliticas.GetPolicyAsync(PoliticasAutorizacionRubricas.CoordinadorPrograma);
        var politicaLectura = await proveedorPoliticas.GetPolicyAsync(PoliticasAutorizacionRubricas.LecturaRubrica);

        politicaCoordinador.Should().NotBeNull();
        politicaLectura.Should().NotBeNull();

        var coordinador = CrearPrincipal("COORDINADOR_PROGRAMA");
        var adminFinanciero = CrearPrincipal("ADMIN_FINANCIERO");
        var miembroComite = CrearPrincipal("MIEMBRO_COMITE");

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaCoordinador!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaLectura!)).Succeeded.Should().BeTrue();

        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaCoordinador!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaLectura!)).Succeeded.Should().BeFalse();

        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaCoordinador!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaLectura!)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_PermitirLectura200AMiembroComiteYDenegar403EnEscrituraYMantenerAdminFinancieroEn403()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = RubricaEvaluacion.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rúbrica Autorización",
            DateTimeOffset.UtcNow,
            usuarioId);

        var repoRubricas = Substitute.For<IRubricaRepository>();
        repoRubricas.ObtenerPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repoRubricas: repoRubricas);

        using var clienteComite = fabrica.CreateClient();
        clienteComite.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        clienteComite.DefaultRequestHeaders.Add(HeaderRol, "MIEMBRO_COMITE");

        var lecturaComite = await clienteComite.GetAsync($"/api/v1/rubricas/{rubrica.Id}");
        lecturaComite.StatusCode.Should().Be(HttpStatusCode.OK);

        var escrituraComite = await clienteComite.PostAsJsonAsync(
            "/api/v1/rubricas",
            new
            {
                programaBecaId = Guid.NewGuid(),
                convocatoriaId = Guid.NewGuid(),
                nombre = "Intento Comité"
            });
        escrituraComite.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var docComite = JsonDocument.Parse(await escrituraComite.Content.ReadAsStringAsync()))
        {
            docComite.RootElement.GetProperty("codigo").GetString()
                .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
        }

        using var clienteFinanciero = fabrica.CreateClient();
        clienteFinanciero.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        clienteFinanciero.DefaultRequestHeaders.Add(HeaderRol, "ADMIN_FINANCIERO");

        var lecturaFinanciero = await clienteFinanciero.GetAsync($"/api/v1/rubricas/{rubrica.Id}");
        lecturaFinanciero.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var docLecturaFin = JsonDocument.Parse(await lecturaFinanciero.Content.ReadAsStringAsync()))
        {
            docLecturaFin.RootElement.GetProperty("codigo").GetString()
                .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
        }

        var escrituraFinanciero = await clienteFinanciero.PostAsJsonAsync(
            "/api/v1/rubricas",
            new
            {
                programaBecaId = Guid.NewGuid(),
                convocatoriaId = Guid.NewGuid(),
                nombre = "Intento Financiero"
            });
        escrituraFinanciero.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var docEscrituraFin = JsonDocument.Parse(await escrituraFinanciero.Content.ReadAsStringAsync()))
        {
            docEscrituraFin.RootElement.GetProperty("codigo").GetString()
                .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
        }
    }

    [Theory]
    [InlineData(PoliticasAutorizacionRubricas.CoordinadorPrograma)]
    [InlineData(PoliticasAutorizacionRubricas.LecturaRubrica)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_EndpointTienePoliticaDeRubricasYResultadoEsForbidden(string politica)
    {
        var manejador = new ManejadorResultadoAutorizacionRubricas();
        var contexto = CrearContextoConPolitica(politica);
        var politicaAutorizacion = new AuthorizationPolicyBuilder().RequireRole("COORDINADOR_PROGRAMA").Build();
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
    public async Task Debe_DelegarEnComportamientoPredeterminado_Cuando_EndpointNoTienePoliticaDeRubricas()
    {
        var manejador = new ManejadorResultadoAutorizacionRubricas();
        var contexto = CrearContextoConPolitica("OtraPolitica");
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddAuthentication(EsquemaPrueba)
            .AddScheme<AuthenticationSchemeOptions, ManejadorAutenticacionPrueba>(EsquemaPrueba, _ => { });
        contexto.RequestServices = servicios.BuildServiceProvider();

        var politicaAutorizacion = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var resultadoEvaluacion = PolicyAuthorizationResult.Forbid();

        await manejador.HandleAsync(
            _ => Task.CompletedTask,
            contexto,
            politicaAutorizacion,
            resultadoEvaluacion);

        contexto.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        contexto.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task Debe_ConservarComportamientoActualDePostulantes_Cuando_SeRegistraAutorizacionDeRubricas()
    {
        var usuarioId = Guid.NewGuid();
        var postulante = Postulante.Crear(
            personaId: null,
            usuarioId: usuarioId,
            numeroDocumentoIdentidad: "V-20202020",
            nombres: "Luis",
            apellidos: "Gómez",
            datosContacto: new DatosContacto("luis@correo.com", "+584121234567"),
            universidadAliadaId: Guid.NewGuid(),
            carreraId: Guid.NewGuid(),
            periodoActualId: Guid.NewGuid(),
            condicionAcademica: CondicionAcademicaIngreso.Matriculado,
            aceptaTratamientoDatos: true,
            fechaRegistro: DateTimeOffset.UtcNow);

        var repo = Substitute.For<IPostulanteRepository>();
        repo.ObtenerPorUsuarioIdAsync(usuarioId, Arg.Any<CancellationToken>())
            .Returns(postulante);

        using var fabrica = CrearFabrica(repo);
        using var cliente = fabrica.CreateClient();

        var respuestaSinToken = await cliente.GetAsync("/api/v1/postulantes/mio");
        respuestaSinToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "ADMIN_FINANCIERO");

        var respuestaAutenticada = await cliente.GetAsync("/api/v1/postulantes/mio");
        respuestaAutenticada.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IPostulanteRepository? repo = null,
        IRubricaRepository? repoRubricas = null)
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
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IUnitOfWork>();

                servicios.AddSingleton(repo ?? Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(repoRubricas ?? Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
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

    private static DefaultHttpContext CrearContextoConPolitica(string politica)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/v1/rubricas";
        contexto.Response.Body = new MemoryStream();
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute(politica)),
            "EndpointPrueba");
        contexto.SetEndpoint(endpoint);
        return contexto;
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
