using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
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

public sealed class PostulacionesInternasControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string HeaderScope = "X-Test-Scope";
    private const string RolCoordinador = "COORDINADOR_PROGRAMA";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public PostulacionesInternasControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    private static ResultadoConsolidadoRanking CrearResultado(Guid postulacionId, decimal puntaje = 90.00m)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId: Guid.NewGuid(),
            postulacionId: postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: 5.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        return ResultadoConsolidadoRanking.Crear(
            calculo,
            DateTimeOffset.UtcNow.AddMinutes(-20),
            Guid.NewGuid());
    }

    [Fact]
    public async Task Debe_Retornar200OkConAdjudicableTrue_Cuando_PostulacionEstaConfirmadaDentroDeCupo()
    {
        var postulacionId = Guid.NewGuid();
        var resultado = CrearResultado(postulacionId);
        resultado.ConfirmarRanking(
            posicionRanking: 1,
            montoEstimadoBeca: 1500.00m,
            clasificacion: ClasificacionRanking.DentroDeCupo,
            fechaConfirmacion: DateTimeOffset.UtcNow.AddMinutes(-5),
            usuarioConfirmadorId: Guid.NewGuid());

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ObtenerPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(resultado);

        using var fabrica = CrearFabrica(repoResultados);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderScope, "postulaciones.internal.read");

        var respuesta = await cliente.GetAsync($"/internal/v1/postulaciones/{postulacionId}/adjudicable");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<AdjudicabilidadPostulacionDto>();
        dto.Should().NotBeNull();
        dto!.PostulacionId.Should().Be(postulacionId);
        dto.Adjudicable.Should().BeTrue();
        dto.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
        dto.Clasificacion.Should().Be("DENTRO_DE_CUPO");
        dto.PosicionRanking.Should().Be(1);
        dto.MontoEstimadoBeca.Should().Be(1500.00m);
        dto.MotivoNoAdjudicable.Should().BeNull();
    }

    [Fact]
    public async Task Debe_Retornar200OkConAdjudicableFalseYMotivo_Cuando_PostulacionEstaProvisionalOEnListaDeEspera()
    {
        var postulacionProvisionalId = Guid.NewGuid();
        var postulacionListaEsperaId = Guid.NewGuid();

        var resultadoProvisional = CrearResultado(postulacionProvisionalId);
        var resultadoListaEspera = CrearResultado(postulacionListaEsperaId, 78.00m);
        resultadoListaEspera.ConfirmarRanking(
            posicionRanking: 4,
            montoEstimadoBeca: 1200.00m,
            clasificacion: ClasificacionRanking.ListaDeEspera,
            fechaConfirmacion: DateTimeOffset.UtcNow.AddMinutes(-5),
            usuarioConfirmadorId: Guid.NewGuid());

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ObtenerPorPostulacionAsync(postulacionProvisionalId, Arg.Any<CancellationToken>())
            .Returns(resultadoProvisional);
        repoResultados.ObtenerPorPostulacionAsync(postulacionListaEsperaId, Arg.Any<CancellationToken>())
            .Returns(resultadoListaEspera);

        using var fabrica = CrearFabrica(repoResultados);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, RolCoordinador);

        var respProvisional = await cliente.GetAsync(
            $"/internal/v1/postulaciones/{postulacionProvisionalId}/adjudicable");
        respProvisional.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoProvisional = await respProvisional.Content.ReadFromJsonAsync<AdjudicabilidadPostulacionDto>();
        dtoProvisional!.Adjudicable.Should().BeFalse();
        dtoProvisional.MotivoNoAdjudicable.Should().Be("RANKING_NO_CONFIRMADO");

        var respListaEspera = await cliente.GetAsync(
            $"/internal/v1/postulaciones/{postulacionListaEsperaId}/adjudicable");
        respListaEspera.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoListaEspera = await respListaEspera.Content.ReadFromJsonAsync<AdjudicabilidadPostulacionDto>();
        dtoListaEspera!.Adjudicable.Should().BeFalse();
        dtoListaEspera.MotivoNoAdjudicable.Should().Be("FUERA_DE_CUPO_EN_LISTA_DE_ESPERA");
    }

    [Fact]
    public async Task Debe_Retornar400Y404_Cuando_PostulacionIdEsVacioONoTieneResultadoConsolidado()
    {
        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();

        using var fabrica = CrearFabrica(repoResultados);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, RolCoordinador);

        var resp400 = await cliente.GetAsync($"/internal/v1/postulaciones/{Guid.Empty}/adjudicable");
        resp400.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var resp404 = await cliente.GetAsync($"/internal/v1/postulaciones/{Guid.NewGuid()}/adjudicable");
        resp404.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var doc = JsonDocument.Parse(await resp404.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("codigo").GetString()
            .Should().Be(ResultadoConsolidadoNoEncontradoException.CodigoError);
    }

    private WebApplicationFactory<Program> CrearFabrica(IResultadoConsolidadoRepository? repoResultados = null)
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
                servicios.AddSingleton(repoResultados ?? Substitute.For<IResultadoConsolidadoRepository>());
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

            var identidad = new ClaimsIdentity(claims, EsquemaPrueba, "sub", "roles");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidad), EsquemaPrueba)));
        }
    }
}
