using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
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

public sealed class AutorizacionComitesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public AutorizacionComitesTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_RegistrarPoliticaGestionComitesEvaluadoresConAccesoExclusivoParaCoordinador()
    {
        PoliticasAutorizacionComites.GestionComitesEvaluadores.Should().Be("GestionComitesEvaluadores");
        PoliticasAutorizacionComites.RolCoordinadorPrograma.Should().Be("COORDINADOR_PROGRAMA");

        using var fabrica = CrearFabrica();
        using var scope = fabrica.Services.CreateScope();
        var proveedorPoliticas = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
        var servicioAutorizacion = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var politica = await proveedorPoliticas.GetPolicyAsync(PoliticasAutorizacionComites.GestionComitesEvaluadores);

        politica.Should().NotBeNull();

        var coordinador = CrearPrincipal("COORDINADOR_PROGRAMA");
        var adminFinanciero = CrearPrincipal("ADMIN_FINANCIERO");
        var miembroComite = CrearPrincipal("MIEMBRO_COMITE");

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politica!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politica!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politica!)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_EndpointTienePoliticaDeComitesYResultadoEsForbidden()
    {
        var manejador = new ManejadorResultadoAutorizacionRubricas();
        var contexto = CrearContextoConPolitica(PoliticasAutorizacionComites.GestionComitesEvaluadores);
        var politicaAutorizacion = new AuthorizationPolicyBuilder()
            .RequireRole(PoliticasAutorizacionComites.RolCoordinadorPrograma)
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

    [Theory]
    [InlineData("ADMIN_FINANCIERO")]
    [InlineData("MIEMBRO_COMITE")]
    public async Task Debe_Denegar403ConPermisoDenegadoEnLosCincoEndpoints_Cuando_RolNoEsCoordinadorPrograma(string rol)
    {
        var usuarioId = Guid.NewGuid();
        var comiteId = Guid.NewGuid();
        var miembroId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, rol);

        var respuestas = new[]
        {
            await cliente.PostAsJsonAsync(
                "/api/v1/comites-evaluadores",
                new
                {
                    programaBecaId = programaId,
                    convocatoriaId,
                    nombre = "Comité Beca",
                    jefaturaUsuarioId = Guid.NewGuid()
                }),
            await cliente.GetAsync($"/api/v1/comites-evaluadores/{comiteId}"),
            await cliente.GetAsync($"/api/v1/comites-evaluadores/programas/{programaId}/convocatorias/{convocatoriaId}"),
            await cliente.PostAsJsonAsync(
                $"/api/v1/comites-evaluadores/{comiteId}/miembros",
                new
                {
                    usuarioEvaluadorId = Guid.NewGuid(),
                    tipo = "INTERNO"
                }),
            await cliente.DeleteAsync($"/api/v1/comites-evaluadores/{comiteId}/miembros/{miembroId}")
        };

        foreach (var respuesta in respuestas)
        {
            respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            documento.RootElement.GetProperty("codigo").GetString()
                .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
        }
    }

    [Fact]
    public async Task Debe_Retornar401UnauthorizedEnLosCincoEndpoints_Cuando_NoHayToken()
    {
        var comiteId = Guid.NewGuid();
        var miembroId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuestas = new[]
        {
            await cliente.PostAsJsonAsync(
                "/api/v1/comites-evaluadores",
                new
                {
                    programaBecaId = programaId,
                    convocatoriaId,
                    nombre = "Comité Beca",
                    jefaturaUsuarioId = Guid.NewGuid()
                }),
            await cliente.GetAsync($"/api/v1/comites-evaluadores/{comiteId}"),
            await cliente.GetAsync($"/api/v1/comites-evaluadores/programas/{programaId}/convocatorias/{convocatoriaId}"),
            await cliente.PostAsJsonAsync(
                $"/api/v1/comites-evaluadores/{comiteId}/miembros",
                new
                {
                    usuarioEvaluadorId = Guid.NewGuid(),
                    tipo = "INTERNO"
                }),
            await cliente.DeleteAsync($"/api/v1/comites-evaluadores/{comiteId}/miembros/{miembroId}")
        };

        foreach (var respuesta in respuestas)
        {
            respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Debe_PermitirAccesoACoordinadorProgramaEnLosEndpointsDeComites()
    {
        var usuarioId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var jefaturaId = Guid.NewGuid();
        var evaluadorId = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            programaId,
            convocatoriaId,
            "Comité Autorización",
            jefaturaId,
            ahora,
            usuarioId);
        var miembro = comite.AgregarMiembro(evaluadorId, TipoEvaluador.Interno, ahora.AddMinutes(1), usuarioId);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ExistePorProgramaYConvocatoriaAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);
        repoComites.ObtenerPorProgramaYConvocatoriaAsync(programaId, convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(comite);
        repoComites.ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        using var fabrica = CrearFabrica(repoComites: repoComites);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "COORDINADOR_PROGRAMA");

        var respCrear = await cliente.PostAsJsonAsync(
            "/api/v1/comites-evaluadores",
            new
            {
                programaBecaId = Guid.NewGuid(),
                convocatoriaId = Guid.NewGuid(),
                nombre = "Nuevo Comité",
                jefaturaUsuarioId = jefaturaId
            });
        respCrear.StatusCode.Should().Be(HttpStatusCode.Created);

        var respPorId = await cliente.GetAsync($"/api/v1/comites-evaluadores/{comite.Id}");
        respPorId.StatusCode.Should().Be(HttpStatusCode.OK);

        var respPorPrograma = await cliente.GetAsync(
            $"/api/v1/comites-evaluadores/programas/{programaId}/convocatorias/{convocatoriaId}");
        respPorPrograma.StatusCode.Should().Be(HttpStatusCode.OK);

        var respAgregar = await cliente.PostAsJsonAsync(
            $"/api/v1/comites-evaluadores/{comite.Id}/miembros",
            new
            {
                usuarioEvaluadorId = Guid.NewGuid(),
                tipo = "EXTERNO"
            });
        respAgregar.StatusCode.Should().Be(HttpStatusCode.Created);

        var respDesvincular = await cliente.DeleteAsync(
            $"/api/v1/comites-evaluadores/{comite.Id}/miembros/{miembro.Id}");
        respDesvincular.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Debe_ConservarComportamientoActualDePostulantes_Cuando_SeRegistraAutorizacionDeComites()
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

        using var fabrica = CrearFabrica(repo: repo);
        using var cliente = fabrica.CreateClient();

        var respuestaSinToken = await cliente.GetAsync("/api/v1/postulantes/mio");
        respuestaSinToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "MIEMBRO_COMITE");

        var respuestaAutenticada = await cliente.GetAsync("/api/v1/postulantes/mio");
        respuestaAutenticada.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IPostulanteRepository? repo = null,
        IComiteEvaluadorRepository? repoComites = null)
    {
        return _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<IPostulanteRepository>();
                servicios.RemoveAll<IRubricaRepository>();
                servicios.RemoveAll<IComiteEvaluadorRepository>();
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IUnitOfWork>();

                servicios.AddSingleton(repo ?? Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(repoComites ?? Substitute.For<IComiteEvaluadorRepository>());
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
        contexto.Request.Path = "/api/v1/comites-evaluadores";
        contexto.Response.Body = new MemoryStream();
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute(politica)),
            "EndpointComitesPrueba");
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
