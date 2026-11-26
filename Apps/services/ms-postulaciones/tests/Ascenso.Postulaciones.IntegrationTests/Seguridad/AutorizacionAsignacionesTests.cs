using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
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

public sealed class AutorizacionAsignacionesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public AutorizacionAsignacionesTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_RegistrarPoliticasDeAsignacionesConRolesLiteralmenteDefinidos()
    {
        PoliticasAutorizacionAsignaciones.GestionAsignacionesEvaluacion.Should().Be("GestionAsignacionesEvaluacion");
        PoliticasAutorizacionAsignaciones.ConsultaMisAsignaciones.Should().Be("ConsultaMisAsignaciones");
        PoliticasAutorizacionAsignaciones.RolCoordinadorPrograma.Should().Be("COORDINADOR_PROGRAMA");
        PoliticasAutorizacionAsignaciones.RolMiembroComite.Should().Be("MIEMBRO_COMITE");

        using var fabrica = CrearFabrica();
        using var scope = fabrica.Services.CreateScope();
        var proveedorPoliticas = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
        var servicioAutorizacion = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var politicaGestion = await proveedorPoliticas.GetPolicyAsync(
            PoliticasAutorizacionAsignaciones.GestionAsignacionesEvaluacion);
        var politicaMisAsignaciones = await proveedorPoliticas.GetPolicyAsync(
            PoliticasAutorizacionAsignaciones.ConsultaMisAsignaciones);

        politicaGestion.Should().NotBeNull();
        politicaMisAsignaciones.Should().NotBeNull();

        var coordinador = CrearPrincipal("COORDINADOR_PROGRAMA");
        var miembroComite = CrearPrincipal("MIEMBRO_COMITE");
        var adminFinanciero = CrearPrincipal("ADMIN_FINANCIERO");

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaGestion!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaGestion!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaGestion!)).Succeeded.Should().BeFalse();

        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaMisAsignaciones!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaMisAsignaciones!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaMisAsignaciones!)).Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(PoliticasAutorizacionAsignaciones.GestionAsignacionesEvaluacion)]
    [InlineData(PoliticasAutorizacionAsignaciones.ConsultaMisAsignaciones)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_EndpointTienePoliticaDeAsignacionesYResultadoEsForbidden(
        string politica)
    {
        var manejador = new ManejadorResultadoAutorizacionRubricas();
        var contexto = CrearContextoConPolitica(politica);
        var politicaAutorizacion = new AuthorizationPolicyBuilder()
            .RequireRole(PoliticasAutorizacionAsignaciones.RolCoordinadorPrograma)
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
    public async Task Debe_Denegar403ConPermisoDenegadoEnLosCuatroEndpoints_Cuando_RolEsAdminFinanciero()
    {
        var usuarioId = Guid.NewGuid();
        var comiteId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "ADMIN_FINANCIERO");

        var respuestas = new[]
        {
            await cliente.PostAsJsonAsync(
                "/api/v1/asignaciones-evaluacion",
                new
                {
                    comiteId,
                    postulacionId,
                    fechaLimiteEvaluacion = DateTimeOffset.UtcNow.AddDays(5),
                    usuariosEvaluadoresIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
                }),
            await cliente.GetAsync($"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}"),
            await cliente.GetAsync($"/api/v1/asignaciones-evaluacion/comites/{comiteId}/carga"),
            await cliente.GetAsync("/api/v1/asignaciones-evaluacion/mis-asignaciones")
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
    public async Task Debe_Denegar403AMiembroComiteEnRutasDeCoordinadorYACoordinadorEnMisAsignaciones()
    {
        var usuarioId = Guid.NewGuid();
        var comiteId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        using var fabrica = CrearFabrica();

        using var clienteMiembro = fabrica.CreateClient();
        clienteMiembro.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        clienteMiembro.DefaultRequestHeaders.Add(HeaderRol, "MIEMBRO_COMITE");

        var respuestasMiembroEnCoordinador = new[]
        {
            await clienteMiembro.PostAsJsonAsync(
                "/api/v1/asignaciones-evaluacion",
                new
                {
                    comiteId,
                    postulacionId,
                    fechaLimiteEvaluacion = DateTimeOffset.UtcNow.AddDays(5),
                    usuariosEvaluadoresIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
                }),
            await clienteMiembro.GetAsync($"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}"),
            await clienteMiembro.GetAsync($"/api/v1/asignaciones-evaluacion/comites/{comiteId}/carga")
        };

        foreach (var respuesta in respuestasMiembroEnCoordinador)
        {
            respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            documento.RootElement.GetProperty("codigo").GetString()
                .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
        }

        using var clienteCoordinador = fabrica.CreateClient();
        clienteCoordinador.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        clienteCoordinador.DefaultRequestHeaders.Add(HeaderRol, "COORDINADOR_PROGRAMA");

        var respuestaCoordinadorEnMisAsignaciones = await clienteCoordinador.GetAsync(
            "/api/v1/asignaciones-evaluacion/mis-asignaciones");
        respuestaCoordinadorEnMisAsignaciones.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var docCoordinador = JsonDocument.Parse(await respuestaCoordinadorEnMisAsignaciones.Content.ReadAsStringAsync());
        docCoordinador.RootElement.GetProperty("codigo").GetString()
            .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
    }

    [Fact]
    public async Task Debe_Retornar401UnauthorizedEnLosCuatroEndpoints_Cuando_NoHayToken()
    {
        var comiteId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuestas = new[]
        {
            await cliente.PostAsJsonAsync(
                "/api/v1/asignaciones-evaluacion",
                new
                {
                    comiteId,
                    postulacionId,
                    fechaLimiteEvaluacion = DateTimeOffset.UtcNow.AddDays(5),
                    usuariosEvaluadoresIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
                }),
            await cliente.GetAsync($"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}"),
            await cliente.GetAsync($"/api/v1/asignaciones-evaluacion/comites/{comiteId}/carga"),
            await cliente.GetAsync("/api/v1/asignaciones-evaluacion/mis-asignaciones")
        };

        foreach (var respuesta in respuestas)
        {
            respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Debe_PermitirAccesoACoordinadorEnRutasDeGestionYAMiembroComiteEnMisAsignaciones()
    {
        var coordinadorId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var evaluador2Id = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Asignaciones",
            evaluador1Id,
            ahora,
            coordinadorId);
        comite.AgregarMiembro(evaluador2Id, TipoEvaluador.Externo, ahora.AddMinutes(1), coordinadorId);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);
        repoAsignaciones.ListarPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);
        repoAsignaciones.ObtenerCargaPorMiembroAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());
        repoAsignaciones.ListarPorEvaluadorAsync(
                evaluador1Id,
                Arg.Any<EstadoAsignacionEvaluacion?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaginaResultado<AsignacionEvaluacion>([], 0));

        using var fabrica = CrearFabrica(repoComites: repoComites, repoAsignaciones: repoAsignaciones);

        using var clienteCoordinador = fabrica.CreateClient();
        clienteCoordinador.DefaultRequestHeaders.Add(HeaderSub, coordinadorId.ToString());
        clienteCoordinador.DefaultRequestHeaders.Add(HeaderRol, "COORDINADOR_PROGRAMA");

        var respAsignar = await clienteCoordinador.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = comite.Id,
                postulacionId,
                fechaLimiteEvaluacion = ahora.AddDays(5),
                usuariosEvaluadoresIds = new[] { evaluador1Id, evaluador2Id }
            });
        respAsignar.StatusCode.Should().Be(HttpStatusCode.Created);

        var respPorPostulacion = await clienteCoordinador.GetAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}");
        respPorPostulacion.StatusCode.Should().Be(HttpStatusCode.OK);

        var respCarga = await clienteCoordinador.GetAsync(
            $"/api/v1/asignaciones-evaluacion/comites/{comite.Id}/carga");
        respCarga.StatusCode.Should().Be(HttpStatusCode.OK);

        using var clienteMiembro = fabrica.CreateClient();
        clienteMiembro.DefaultRequestHeaders.Add(HeaderSub, evaluador1Id.ToString());
        clienteMiembro.DefaultRequestHeaders.Add(HeaderRol, "MIEMBRO_COMITE");

        var respMisAsignaciones = await clienteMiembro.GetAsync(
            "/api/v1/asignaciones-evaluacion/mis-asignaciones");
        respMisAsignaciones.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Debe_ConservarComportamientoActualDePostulantes_Cuando_SeRegistraAutorizacionDeAsignaciones()
    {
        var usuarioId = Guid.NewGuid();
        var postulante = Postulante.Crear(
            personaId: null,
            usuarioId: usuarioId,
            numeroDocumentoIdentidad: "V-30303030",
            nombres: "María",
            apellidos: "Salazar",
            datosContacto: new DatosContacto("maria@correo.com", "+584121234567"),
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
        IComiteEvaluadorRepository? repoComites = null,
        IAsignacionEvaluacionRepository? repoAsignaciones = null)
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

                servicios.AddSingleton(repo ?? Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(repoComites ?? Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(repoAsignaciones ?? Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
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
        contexto.Request.Path = "/api/v1/asignaciones-evaluacion";
        contexto.Response.Body = new MemoryStream();
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute(politica)),
            "EndpointAsignacionesPrueba");
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
