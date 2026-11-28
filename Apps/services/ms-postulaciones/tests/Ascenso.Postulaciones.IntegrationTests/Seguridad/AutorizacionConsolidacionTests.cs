using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
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

public sealed class AutorizacionConsolidacionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public AutorizacionConsolidacionTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_RegistrarPoliticasDeConsolidacionConRolesLiteralmenteDefinidos()
    {
        PoliticasAutorizacionConsolidacion.ConsultaConsolidacion.Should().Be("ConsultaConsolidacion");
        PoliticasAutorizacionConsolidacion.GestionConsolidacion.Should().Be("GestionConsolidacion");
        PoliticasAutorizacionConsolidacion.RolCoordinadorPrograma.Should().Be("COORDINADOR_PROGRAMA");
        PoliticasAutorizacionConsolidacion.RolDireccion.Should().Be("DIRECCION");

        using var fabrica = CrearFabrica();
        using var scope = fabrica.Services.CreateScope();
        var proveedorPoliticas = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
        var servicioAutorizacion = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var politicaConsulta = await proveedorPoliticas.GetPolicyAsync(
            PoliticasAutorizacionConsolidacion.ConsultaConsolidacion);
        var politicaGestion = await proveedorPoliticas.GetPolicyAsync(
            PoliticasAutorizacionConsolidacion.GestionConsolidacion);

        politicaConsulta.Should().NotBeNull();
        politicaGestion.Should().NotBeNull();

        var coordinador = CrearPrincipal("COORDINADOR_PROGRAMA");
        var direccion = CrearPrincipal("DIRECCION");
        var miembroComite = CrearPrincipal("MIEMBRO_COMITE");
        var adminFinanciero = CrearPrincipal("ADMIN_FINANCIERO");

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaConsulta!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(direccion, null, politicaConsulta!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaConsulta!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaConsulta!)).Succeeded.Should().BeFalse();

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaGestion!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(direccion, null, politicaGestion!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaGestion!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaGestion!)).Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(PoliticasAutorizacionConsolidacion.ConsultaConsolidacion)]
    [InlineData(PoliticasAutorizacionConsolidacion.GestionConsolidacion)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_EndpointTienePoliticaDeConsolidacionYResultadoEsForbidden(
        string politica)
    {
        var manejador = new ManejadorResultadoAutorizacionRubricas();
        var contexto = CrearContextoConPolitica(politica);
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
    public async Task Debe_PermitirAccesoACoordinadorProgramaEnLosCuatroEndpointsDeConsolidacion()
    {
        var (postulacionId, convocatoriaId, repoResultados, repoDatos) = CrearMocksEscenarioActivo();
        var coordinadorId = Guid.NewGuid();

        using var fabrica = CrearFabrica(repoResultados: repoResultados, repoDatos: repoDatos);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, coordinadorId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "COORDINADOR_PROGRAMA");

        var respConsolidar = await cliente.PostAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}",
            content: null);
        respConsolidar.StatusCode.Should().Be(HttpStatusCode.Created);

        var respObtener = await cliente.GetAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}");
        respObtener.StatusCode.Should().Be(HttpStatusCode.OK);

        var respListar = await cliente.GetAsync(
            $"/api/v1/resultados-consolidados/convocatorias/{convocatoriaId}");
        respListar.StatusCode.Should().Be(HttpStatusCode.OK);

        var respRevisar = await cliente.PostAsJsonAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar",
            new { comentario = "Revisión validada por coordinación de programa." });
        respRevisar.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Debe_PermitirLectura200ADireccionEnLosDosGetYDenegar403PermisoDenegadoEnLosDosPost()
    {
        var (postulacionId, convocatoriaId, repoResultados, repoDatos) = CrearMocksEscenarioActivo();
        var direccionId = Guid.NewGuid();

        using var fabrica = CrearFabrica(repoResultados: repoResultados, repoDatos: repoDatos);
        using var clienteCoordinador = fabrica.CreateClient();
        clienteCoordinador.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
        clienteCoordinador.DefaultRequestHeaders.Add(HeaderRol, "COORDINADOR_PROGRAMA");

        await clienteCoordinador.PostAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}",
            content: null);

        using var clienteDireccion = fabrica.CreateClient();
        clienteDireccion.DefaultRequestHeaders.Add(HeaderSub, direccionId.ToString());
        clienteDireccion.DefaultRequestHeaders.Add(HeaderRol, "DIRECCION");

        var respObtener = await clienteDireccion.GetAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}");
        respObtener.StatusCode.Should().Be(HttpStatusCode.OK);

        var respListar = await clienteDireccion.GetAsync(
            $"/api/v1/resultados-consolidados/convocatorias/{convocatoriaId}");
        respListar.StatusCode.Should().Be(HttpStatusCode.OK);

        var respConsolidar = await clienteDireccion.PostAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}",
            content: null);
        await VerificarPermisoDenegadoAsync(respConsolidar);

        var respRevisar = await clienteDireccion.PostAsJsonAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar",
            new { comentario = "Intento de revisión por dirección sin permiso de escritura." });
        await VerificarPermisoDenegadoAsync(respRevisar);
    }

    [Theory]
    [InlineData("MIEMBRO_COMITE")]
    [InlineData("ADMIN_FINANCIERO")]
    public async Task Debe_Denegar403ConPermisoDenegadoEnLosCuatroEndpoints_Cuando_RolEsMiembroComiteOAdminFinanciero(
        string rol)
    {
        var usuarioId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, rol);

        var respuestas = new[]
        {
            await cliente.PostAsync(
                $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}",
                content: null),
            await cliente.PostAsJsonAsync(
                $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar",
                new { comentario = "Intento de revisión con rol no autorizado." }),
            await cliente.GetAsync(
                $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}"),
            await cliente.GetAsync(
                $"/api/v1/resultados-consolidados/convocatorias/{convocatoriaId}")
        };

        foreach (var respuesta in respuestas)
        {
            await VerificarPermisoDenegadoAsync(respuesta);
        }
    }

    [Fact]
    public async Task Debe_Retornar401UnauthorizedEnLosCuatroEndpoints_Cuando_NoHayToken()
    {
        var postulacionId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuestas = new[]
        {
            await cliente.PostAsync(
                $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}",
                content: null),
            await cliente.PostAsJsonAsync(
                $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar",
                new { comentario = "Intento sin token de autenticación." }),
            await cliente.GetAsync(
                $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}"),
            await cliente.GetAsync(
                $"/api/v1/resultados-consolidados/convocatorias/{convocatoriaId}")
        };

        foreach (var respuesta in respuestas)
        {
            respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    private static (
        Guid PostulacionId,
        Guid ConvocatoriaId,
        IResultadoConsolidadoRepository RepoResultados,
        IConsultaDatosConsolidacionRepository RepoDatos) CrearMocksEscenarioActivo()
    {
        var ahora = DateTimeOffset.UtcNow;
        var programaBecaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var evaluador2Id = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        var rubrica = RubricaEvaluacion.Crear(
            programaBecaId,
            convocatoriaId,
            "Rúbrica Seguridad Consolidación",
            ahora.AddHours(-3),
            coordinadorId);
        var criterio = rubrica.AgregarCriterio(
            "Mérito",
            "Puntaje sobre 20",
            100.00m,
            0.00m,
            20.00m,
            ahora.AddHours(-3).AddMinutes(1),
            coordinadorId);
        rubrica.Publicar(ahora.AddHours(-3).AddMinutes(2), coordinadorId);
        rubrica.Bloquear(MotivoBloqueoRubrica.PrimeraCalificacion, ahora.AddHours(-2), coordinadorId);

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Seguridad Consolidación",
            evaluador1Id,
            ahora.AddHours(-2),
            coordinadorId);
        var miembro1 = comite.Miembros.First();
        var miembro2 = comite.AgregarMiembro(
            evaluador2Id,
            TipoEvaluador.Externo,
            ahora.AddHours(-2).AddMinutes(1),
            coordinadorId);

        var asignacion1 = AsignacionEvaluacion.Crear(
            comite,
            miembro1.Id,
            postulacionId,
            ahora.AddHours(-1),
            ahora.AddDays(5),
            coordinadorId);
        asignacion1.IniciarEvaluacion(ahora.AddMinutes(-50), evaluador1Id);
        var evaluacion1 = Evaluacion.CrearBorrador(
            asignacion1,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, 13.00m, "C1")],
            "E1",
            ahora.AddMinutes(-50),
            evaluador1Id);
        evaluacion1.EnviarDefinitiva(rubrica, asignacion1.FechaLimiteEvaluacion, ahora.AddMinutes(-45), evaluador1Id);
        asignacion1.CompletarEvaluacion(ahora.AddMinutes(-45), evaluador1Id);

        var asignacion2 = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            postulacionId,
            ahora.AddHours(-1),
            ahora.AddDays(5),
            coordinadorId);
        asignacion2.IniciarEvaluacion(ahora.AddMinutes(-40), evaluador2Id);
        var evaluacion2 = Evaluacion.CrearBorrador(
            asignacion2,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, 19.00m, "C2")],
            "E2",
            ahora.AddMinutes(-40),
            evaluador2Id);
        evaluacion2.EnviarDefinitiva(rubrica, asignacion2.FechaLimiteEvaluacion, ahora.AddMinutes(-35), evaluador2Id);
        asignacion2.CompletarEvaluacion(ahora.AddMinutes(-35), evaluador2Id);

        ResultadoConsolidadoRanking? resultadoActivo = null;

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ObtenerParaActualizarPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(_ => resultadoActivo);
        repoResultados.ObtenerPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(_ => resultadoActivo);
        repoResultados
            .When(r => r.AgregarAsync(Arg.Any<ResultadoConsolidadoRanking>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => resultadoActivo = callInfo.Arg<ResultadoConsolidadoRanking>());
        repoResultados.ListarPorConvocatoriaAsync(
                convocatoriaId,
                Arg.Any<bool?>(),
                Arg.Any<bool?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => new PaginaResultado<ResultadoConsolidadoRanking>(
                resultadoActivo is null ? [] : [resultadoActivo],
                resultadoActivo is null ? 0 : 1));

        var repoDatos = Substitute.For<IConsultaDatosConsolidacionRepository>();
        repoDatos.ListarAsignacionesVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([asignacion1, asignacion2]);
        repoDatos.ListarEvaluacionesDefinitivasPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([evaluacion1, evaluacion2]);
        repoDatos.ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns((AutorizacionExcepcionEvaluadorUnico?)null);

        return (postulacionId, convocatoriaId, repoResultados, repoDatos);
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
        IConsultaDatosConsolidacionRepository? repoDatos = null)
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

                var proveedorPorDefecto = Substitute.For<IProveedorParametrosConsolidacion>();
                proveedorPorDefecto.ObtenerParametrosAsync(
                        Arg.Any<Guid>(),
                        Arg.Any<Guid>(),
                        Arg.Any<CancellationToken>())
                    .Returns(ParametrosConsolidacion.PorDefecto);

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
                servicios.AddSingleton(repoDatos ?? Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(proveedorPorDefecto);
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(Substitute.For<IEntradaListaEsperaRepository>());
                servicios.AddSingleton(Substitute.For<IGestorConcurrenciaListaEspera>());
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
        contexto.Request.Path = "/api/v1/resultados-consolidados";
        contexto.Response.Body = new MemoryStream();
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute(politica)),
            "EndpointConsolidacionPrueba");
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
