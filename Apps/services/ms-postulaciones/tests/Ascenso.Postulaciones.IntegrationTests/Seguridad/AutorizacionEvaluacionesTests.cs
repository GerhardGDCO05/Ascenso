using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
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

public sealed class AutorizacionEvaluacionesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public AutorizacionEvaluacionesTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_RegistrarPoliticasDeEvaluacionesConRolesLiteralmenteDefinidos()
    {
        PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones.Should().Be("GestionMisEvaluaciones");
        PoliticasAutorizacionEvaluaciones.SupervisionEvaluaciones.Should().Be("SupervisionEvaluaciones");
        PoliticasAutorizacionEvaluaciones.RolCoordinadorPrograma.Should().Be("COORDINADOR_PROGRAMA");
        PoliticasAutorizacionEvaluaciones.RolMiembroComite.Should().Be("MIEMBRO_COMITE");

        using var fabrica = CrearFabrica();
        using var scope = fabrica.Services.CreateScope();
        var proveedorPoliticas = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
        var servicioAutorizacion = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var politicaGestion = await proveedorPoliticas.GetPolicyAsync(
            PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones);
        var politicaSupervision = await proveedorPoliticas.GetPolicyAsync(
            PoliticasAutorizacionEvaluaciones.SupervisionEvaluaciones);

        politicaGestion.Should().NotBeNull();
        politicaSupervision.Should().NotBeNull();

        var miembroComite = CrearPrincipal("MIEMBRO_COMITE");
        var coordinador = CrearPrincipal("COORDINADOR_PROGRAMA");
        var adminFinanciero = CrearPrincipal("ADMIN_FINANCIERO");

        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaGestion!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaGestion!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaGestion!)).Succeeded.Should().BeFalse();

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaSupervision!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaSupervision!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaSupervision!)).Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones)]
    [InlineData(PoliticasAutorizacionEvaluaciones.SupervisionEvaluaciones)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_EndpointTienePoliticaDeEvaluacionesYResultadoEsForbidden(
        string politica)
    {
        var manejador = new ManejadorResultadoAutorizacionRubricas();
        var contexto = CrearContextoConPolitica(politica);
        var politicaAutorizacion = new AuthorizationPolicyBuilder()
            .RequireRole(PoliticasAutorizacionEvaluaciones.RolMiembroComite)
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
    public async Task Debe_Denegar403ConPermisoDenegadoEnLosCincoEndpoints_Cuando_RolEsAdminFinanciero()
    {
        var usuarioId = Guid.NewGuid();
        var asignacionId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "ADMIN_FINANCIERO");

        var respuestas = new[]
        {
            await cliente.PutAsJsonAsync(
                $"/api/v1/evaluaciones/asignaciones/{asignacionId}/borrador",
                new
                {
                    calificaciones = new[]
                    {
                        new { criterioEvaluacionId = Guid.NewGuid(), puntajeAsignado = 10.00m }
                    }
                }),
            await cliente.PostAsync(
                $"/api/v1/evaluaciones/asignaciones/{asignacionId}/enviar",
                content: null),
            await cliente.GetAsync($"/api/v1/evaluaciones/asignaciones/{asignacionId}"),
            await cliente.GetAsync("/api/v1/evaluaciones/mis-evaluaciones"),
            await cliente.GetAsync($"/api/v1/evaluaciones/postulaciones/{postulacionId}")
        };

        foreach (var respuesta in respuestas)
        {
            respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            documento.RootElement.GetProperty("status").GetInt32().Should().Be((int)HttpStatusCode.Forbidden);
            documento.RootElement.GetProperty("codigo").GetString()
                .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
        }
    }

    [Fact]
    public async Task Debe_Denegar403AMiembroComiteEnSupervisionYACoordinadorEnLosCuatroEndpointsDeEvaluador()
    {
        var usuarioId = Guid.NewGuid();
        var asignacionId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        using var fabrica = CrearFabrica();

        using var clienteMiembro = fabrica.CreateClient();
        clienteMiembro.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        clienteMiembro.DefaultRequestHeaders.Add(HeaderRol, "MIEMBRO_COMITE");

        var respMiembroEnSupervision = await clienteMiembro.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}");
        respMiembroEnSupervision.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var docMiembro = JsonDocument.Parse(await respMiembroEnSupervision.Content.ReadAsStringAsync()))
        {
            docMiembro.RootElement.GetProperty("codigo").GetString()
                .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
        }

        using var clienteCoordinador = fabrica.CreateClient();
        clienteCoordinador.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        clienteCoordinador.DefaultRequestHeaders.Add(HeaderRol, "COORDINADOR_PROGRAMA");

        var respuestasCoordinadorEnEvaluador = new[]
        {
            await clienteCoordinador.PutAsJsonAsync(
                $"/api/v1/evaluaciones/asignaciones/{asignacionId}/borrador",
                new
                {
                    calificaciones = new[]
                    {
                        new { criterioEvaluacionId = Guid.NewGuid(), puntajeAsignado = 10.00m }
                    }
                }),
            await clienteCoordinador.PostAsync(
                $"/api/v1/evaluaciones/asignaciones/{asignacionId}/enviar",
                content: null),
            await clienteCoordinador.GetAsync($"/api/v1/evaluaciones/asignaciones/{asignacionId}"),
            await clienteCoordinador.GetAsync("/api/v1/evaluaciones/mis-evaluaciones")
        };

        foreach (var respuesta in respuestasCoordinadorEnEvaluador)
        {
            respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            using var docCoordinador = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            docCoordinador.RootElement.GetProperty("codigo").GetString()
                .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
        }
    }

    [Fact]
    public async Task Debe_Retornar401UnauthorizedEnLosCincoEndpoints_Cuando_NoHayToken()
    {
        var asignacionId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuestas = new[]
        {
            await cliente.PutAsJsonAsync(
                $"/api/v1/evaluaciones/asignaciones/{asignacionId}/borrador",
                new
                {
                    calificaciones = new[]
                    {
                        new { criterioEvaluacionId = Guid.NewGuid(), puntajeAsignado = 10.00m }
                    }
                }),
            await cliente.PostAsync(
                $"/api/v1/evaluaciones/asignaciones/{asignacionId}/enviar",
                content: null),
            await cliente.GetAsync($"/api/v1/evaluaciones/asignaciones/{asignacionId}"),
            await cliente.GetAsync("/api/v1/evaluaciones/mis-evaluaciones"),
            await cliente.GetAsync($"/api/v1/evaluaciones/postulaciones/{postulacionId}")
        };

        foreach (var respuesta in respuestas)
        {
            respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Debe_PermitirAccesoAMiembroComiteEnLosCuatroEndpointsPropiosYACoordinadorEnSupervision()
    {
        var ahora = DateTimeOffset.UtcNow;
        var programaBecaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();
        var evaluadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        var rubrica = RubricaEvaluacion.Crear(
            programaBecaId,
            convocatoriaId,
            "Rúbrica Seguridad 2026",
            ahora.AddHours(-2),
            coordinadorId);
        var criterio = rubrica.AgregarCriterio(
            "Mérito",
            "Evaluación integral",
            100.00m,
            0.00m,
            20.00m,
            ahora.AddHours(-2).AddMinutes(1),
            coordinadorId);
        rubrica.Publicar(ahora.AddHours(-2).AddMinutes(2), coordinadorId);

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Seguridad",
            evaluadorId,
            ahora.AddHours(-1),
            coordinadorId);
        var miembro = comite.Miembros.First();

        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            postulacionId,
            ahora.AddMinutes(-30),
            ahora.AddDays(5),
            coordinadorId);

        Evaluacion? evaluacionActiva = null;

        var repoAsignacion = Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
        repoAsignacion.ObtenerParaActualizarPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(_ => asignacion);
        repoAsignacion.ObtenerPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(_ => asignacion);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoRubrica = Substitute.For<IConsultaRubricaParaEvaluacionRepository>();
        repoRubrica.ObtenerPorProgramaYConvocatoriaAsync(
                programaBecaId,
                convocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(rubrica);

        var repoEvaluacion = Substitute.For<IEvaluacionRepository>();
        repoEvaluacion.ObtenerParaActualizarPorAsignacionAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(_ => evaluacionActiva);
        repoEvaluacion.ObtenerPorAsignacionAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(_ => evaluacionActiva);
        repoEvaluacion
            .When(r => r.AgregarAsync(Arg.Any<Evaluacion>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => evaluacionActiva = callInfo.Arg<Evaluacion>());
        repoEvaluacion.ListarPorEvaluadorAsync(
                evaluadorId,
                Arg.Any<EstadoEvaluacion?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => new PaginaResultado<Evaluacion>(
                evaluacionActiva is null ? [] : [evaluacionActiva],
                evaluacionActiva is null ? 0 : 1));
        repoEvaluacion.ListarDefinitivasPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(_ => evaluacionActiva is null ? [] : [evaluacionActiva]);

        using var fabrica = CrearFabrica(
            repoAsignacion: repoAsignacion,
            repoComites: repoComites,
            repoRubrica: repoRubrica,
            repoEvaluacion: repoEvaluacion);

        using var clienteMiembro = fabrica.CreateClient();
        clienteMiembro.DefaultRequestHeaders.Add(HeaderSub, evaluadorId.ToString());
        clienteMiembro.DefaultRequestHeaders.Add(HeaderRol, "MIEMBRO_COMITE");

        var respPut = await clienteMiembro.PutAsJsonAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/borrador",
            new
            {
                calificaciones = new[]
                {
                    new { criterioEvaluacionId = criterio.Id, puntajeAsignado = 18.00m }
                }
            });
        respPut.StatusCode.Should().Be(HttpStatusCode.Created);

        var respGetAsignacion = await clienteMiembro.GetAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}");
        respGetAsignacion.StatusCode.Should().Be(HttpStatusCode.OK);

        var respMisEvaluaciones = await clienteMiembro.GetAsync(
            "/api/v1/evaluaciones/mis-evaluaciones");
        respMisEvaluaciones.StatusCode.Should().Be(HttpStatusCode.OK);

        var respEnviar = await clienteMiembro.PostAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/enviar",
            content: null);
        respEnviar.StatusCode.Should().Be(HttpStatusCode.OK);

        using var clienteCoordinador = fabrica.CreateClient();
        clienteCoordinador.DefaultRequestHeaders.Add(HeaderSub, coordinadorId.ToString());
        clienteCoordinador.DefaultRequestHeaders.Add(HeaderRol, "COORDINADOR_PROGRAMA");

        var respSupervision = await clienteCoordinador.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}");
        respSupervision.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Debe_ExigirPoliticaGestionMisEvaluacionesEnEndpointDeCoevaluadores_Cuando_SeEvaluaPorRolYSinToken()
    {
        var ahora = DateTimeOffset.UtcNow;
        var programaBecaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();
        var evaluadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Seguridad Coevaluadores",
            evaluadorId,
            ahora.AddDays(-10),
            coordinadorId);
        var miembro = comite.Miembros.First();

        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            postulacionId,
            ahora.AddDays(-9),
            ahora.AddDays(-1),
            coordinadorId);

        var repoCoevaluadores = Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>();
        repoCoevaluadores.ObtenerAsignacionVigenteEvaluadorAsync(
                postulacionId,
                evaluadorId,
                Arg.Any<CancellationToken>())
            .Returns(asignacion);
        repoCoevaluadores.ListarDefinitivasDeCoevaluadoresAsync(
                postulacionId,
                evaluadorId,
                Arg.Any<CancellationToken>())
            .Returns([]);

        var convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();
        convocatoriaClient.ObtenerVentanaRevisionAsync(
                convocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                convocatoriaId,
                EstadoConvocatoriaExterna.Cerrada,
                ahora.AddDays(-8),
                ahora.AddMinutes(-30)));

        using var fabrica = CrearFabrica(
            repoCoevaluadores: repoCoevaluadores,
            convocatoriaClient: convocatoriaClient);

        // 1. Sin token -> 401 Unauthorized
        using var clienteAnonimo = fabrica.CreateClient();
        var respSinToken = await clienteAnonimo.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores");
        respSinToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. ADMIN_FINANCIERO y COORDINADOR_PROGRAMA -> 403 Forbidden con codigo = PERMISO_DENEGADO
        foreach (var rolNoAutorizado in new[] { "ADMIN_FINANCIERO", "COORDINADOR_PROGRAMA" })
        {
            using var clienteNoAutorizado = fabrica.CreateClient();
            clienteNoAutorizado.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
            clienteNoAutorizado.DefaultRequestHeaders.Add(HeaderRol, rolNoAutorizado);

            var respDenegado = await clienteNoAutorizado.GetAsync(
                $"/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores");

            respDenegado.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            using var doc = JsonDocument.Parse(await respDenegado.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("status").GetInt32().Should().Be((int)HttpStatusCode.Forbidden);
            doc.RootElement.GetProperty("codigo").GetString()
                .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
        }

        // 3. MIEMBRO_COMITE -> 200 OK
        using var clienteMiembro = fabrica.CreateClient();
        clienteMiembro.DefaultRequestHeaders.Add(HeaderSub, evaluadorId.ToString());
        clienteMiembro.DefaultRequestHeaders.Add(HeaderRol, "MIEMBRO_COMITE");

        var respAutorizado = await clienteMiembro.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores");
        respAutorizado.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IConsultaAsignacionParaEvaluacionRepository? repoAsignacion = null,
        IComiteEvaluadorRepository? repoComites = null,
        IConsultaRubricaParaEvaluacionRepository? repoRubrica = null,
        IEvaluacionRepository? repoEvaluacion = null,
        IConsultaEvaluacionesCoevaluadoresRepository? repoCoevaluadores = null,
        IConvocatoriaEvaluacionClient? convocatoriaClient = null)
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
                servicios.RemoveAll<IBloqueadorRubricaEvaluacion>();
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IConvocatoriaEvaluacionClient>();
                servicios.RemoveAll<IUnitOfWork>();

                var clienteConvocatoriaPorDefecto = Substitute.For<IConvocatoriaEvaluacionClient>();
                clienteConvocatoriaPorDefecto
                    .ObtenerVentanaRevisionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(callInfo =>
                    {
                        var ahora = DateTimeOffset.UtcNow;
                        return new VentanaRevisionConvocatoria(
                            callInfo.ArgAt<Guid>(0),
                            EstadoConvocatoriaExterna.Publicada,
                            ahora.AddDays(-10),
                            ahora.AddDays(30));
                    });

                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(repoComites ?? Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(repoEvaluacion ?? Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(repoAsignacion ?? Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(repoRubrica ?? Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(repoCoevaluadores ?? Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(Substitute.For<IEntradaListaEsperaRepository>());
                servicios.AddSingleton(Substitute.For<IGestorConcurrenciaListaEspera>());
                servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IBloqueadorRubricaEvaluacion>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(convocatoriaClient ?? clienteConvocatoriaPorDefecto);
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
        contexto.Request.Path = "/api/v1/evaluaciones";
        contexto.Response.Body = new MemoryStream();
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute(politica)),
            "EndpointEvaluacionesPrueba");
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
