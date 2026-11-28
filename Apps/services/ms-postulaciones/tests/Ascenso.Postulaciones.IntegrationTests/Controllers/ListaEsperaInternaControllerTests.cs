using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
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

public sealed class ListaEsperaInternaControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string HeaderScope = "X-Test-Scope";
    private const string HeaderPermiso = "X-Test-Permiso";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public ListaEsperaInternaControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    private static EntradaListaEspera CrearEntrada(
        Guid convocatoriaId,
        Guid programaBecaId,
        Guid postulacionId,
        int posicionOrden = 1)
        => EntradaListaEspera.Crear(
            convocatoriaId,
            programaBecaId,
            postulacionId,
            posicionOrden,
            1500.00m,
            new DateTimeOffset(2026, 11, 27, 9, 0, 0, TimeSpan.Zero),
            Guid.CreateVersion7());

    [Fact]
    public async Task Debe_RegistrarVacanteLiberadaConTokenDeServicioSinSubUsandoUsuarioSistema_Cuando_SeInvocaEndpointInterno()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var entrada = CrearEntrada(convocatoriaId, programaId, Guid.CreateVersion7(), 1);

        var gestor = Substitute.For<IGestorConcurrenciaListaEspera>();
        gestor.EjecutarConBloqueoConvocatoriaAsync(
                convocatoriaId,
                Arg.Any<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<EntradaListaEsperaDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(llamada =>
            {
                var accion = llamada.ArgAt<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<EntradaListaEsperaDto>>>(1);
                return accion([entrada], llamada.ArgAt<CancellationToken>(2));
            });

        using var fabrica = CrearFabrica(gestor: gestor);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderScope, "postulaciones.internal.write");

        var resp = await cliente.PostAsJsonAsync(
            $"/internal/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas",
            new { motivoVacante = "Oferta expirada en MS-BEC." });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<EntradaListaEsperaDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(entrada.Id);
        dto.Estado.Should().Be("ELEGIBLE_POR_VACANTE");
        dto.FechaElegibilidad.Should().NotBeNull();
        dto.FechaPromocion.Should().BeNull();
        entrada.UsuarioModificacionId.Should().Be(UsuarioSistema.Id);
    }

    [Fact]
    public async Task Debe_Retornar400Y404_Cuando_EndpointInternoRecibeConvocatoriaVaciaOSinCandidatos()
    {
        var convocatoriaVaciaId = Guid.CreateVersion7();
        var gestor = Substitute.For<IGestorConcurrenciaListaEspera>();
        gestor.EjecutarConBloqueoConvocatoriaAsync(
                convocatoriaVaciaId,
                Arg.Any<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<EntradaListaEsperaDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(llamada =>
            {
                var accion = llamada.ArgAt<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<EntradaListaEsperaDto>>>(1);
                return accion([], llamada.ArgAt<CancellationToken>(2));
            });

        using var fabrica = CrearFabrica(gestor: gestor);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderPermiso, "postulaciones:escribir");

        var resp400 = await cliente.PostAsync(
            $"/internal/v1/lista-espera/convocatorias/{Guid.Empty}/vacantes-liberadas",
            content: null);
        resp400.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var resp404 = await cliente.PostAsync(
            $"/internal/v1/lista-espera/convocatorias/{convocatoriaVaciaId}/vacantes-liberadas",
            content: null);
        resp404.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var doc = JsonDocument.Parse(await resp404.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("codigo").GetString().Should().Be(ListaEsperaSinCandidatosException.CodigoError);
    }

    [Fact]
    public async Task Debe_RetornarAdjudicableTrueParaPostulacionPromovidaYFalseParaElegiblePorVacante_Cuando_SeConsultaAdjudicableInterno()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var postulacionPromovidaId = Guid.CreateVersion7();
        var postulacionElegibleId = Guid.CreateVersion7();

        var resultadoPromovido = CrearResultadoConfirmadoEnListaEspera(convocatoriaId, postulacionPromovidaId, 3, 1200.00m);
        var resultadoElegible = CrearResultadoConfirmadoEnListaEspera(convocatoriaId, postulacionElegibleId, 4, 1100.00m);

        var entradaPromovida = CrearEntrada(convocatoriaId, programaId, postulacionPromovidaId, 1);
        var fechaPromocion = new DateTimeOffset(2026, 11, 27, 13, 0, 0, TimeSpan.Zero);
        entradaPromovida.Promover(fechaPromocion, coordinadorId, "Promoción aprobada");

        var entradaElegible = CrearEntrada(convocatoriaId, programaId, postulacionElegibleId, 2);
        entradaElegible.MarcarElegiblePorVacante(new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero), UsuarioSistema.Id);

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ObtenerPorPostulacionAsync(postulacionPromovidaId, Arg.Any<CancellationToken>())
            .Returns(resultadoPromovido);
        repoResultados.ObtenerPorPostulacionAsync(postulacionElegibleId, Arg.Any<CancellationToken>())
            .Returns(resultadoElegible);

        var repoLista = Substitute.For<IEntradaListaEsperaRepository>();
        repoLista.ObtenerPorPostulacionAsync(postulacionPromovidaId, Arg.Any<CancellationToken>())
            .Returns(entradaPromovida);
        repoLista.ObtenerPorPostulacionAsync(postulacionElegibleId, Arg.Any<CancellationToken>())
            .Returns(entradaElegible);

        using var fabrica = CrearFabrica(repoLista: repoLista, repoResultados: repoResultados);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderScope, "postulaciones.internal.read");

        var respPromovida = await cliente.GetAsync($"/internal/v1/postulaciones/{postulacionPromovidaId}/adjudicable");
        respPromovida.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoPromovida = await respPromovida.Content.ReadFromJsonAsync<AdjudicabilidadPostulacionDto>();
        dtoPromovida.Should().NotBeNull();
        dtoPromovida!.Adjudicable.Should().BeTrue();
        dtoPromovida.MotivoNoAdjudicable.Should().BeNull();
        dtoPromovida.PromovidaDeListaEspera.Should().BeTrue();
        dtoPromovida.EstadoListaEspera.Should().Be("PROMOVIDA");
        dtoPromovida.PosicionListaEspera.Should().Be(1);
        dtoPromovida.FechaPromocionListaEspera.Should().Be(fechaPromocion);
        dtoPromovida.ProgramaBecaId.Should().Be(programaId);

        var respElegible = await cliente.GetAsync($"/internal/v1/postulaciones/{postulacionElegibleId}/adjudicable");
        respElegible.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoElegible = await respElegible.Content.ReadFromJsonAsync<AdjudicabilidadPostulacionDto>();
        dtoElegible.Should().NotBeNull();
        dtoElegible!.Adjudicable.Should().BeFalse();
        dtoElegible.MotivoNoAdjudicable.Should().Be("FUERA_DE_CUPO_EN_LISTA_DE_ESPERA");
        dtoElegible.PromovidaDeListaEspera.Should().BeFalse();
        dtoElegible.EstadoListaEspera.Should().Be("ELEGIBLE_POR_VACANTE");
        dtoElegible.PosicionListaEspera.Should().Be(2);
    }

    private static ResultadoConsolidadoRanking CrearResultadoConfirmadoEnListaEspera(
        Guid convocatoriaId,
        Guid postulacionId,
        int posicionRanking,
        decimal monto)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId: convocatoriaId,
            postulacionId: postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 84.00m,
            diferenciaEntreEvaluadores: 5.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        var resultado = ResultadoConsolidadoRanking.Crear(
            calculo,
            new DateTimeOffset(2026, 11, 27, 8, 0, 0, TimeSpan.Zero),
            Guid.CreateVersion7());

        resultado.ConfirmarRanking(
            posicionRanking,
            monto,
            ClasificacionRanking.ListaDeEspera,
            new DateTimeOffset(2026, 11, 27, 9, 0, 0, TimeSpan.Zero),
            Guid.CreateVersion7());

        return resultado;
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IEntradaListaEsperaRepository? repoLista = null,
        IGestorConcurrenciaListaEspera? gestor = null,
        IResultadoConsolidadoRepository? repoResultados = null)
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
                servicios.AddSingleton(repoLista ?? Substitute.For<IEntradaListaEsperaRepository>());
                servicios.AddSingleton(gestor ?? Substitute.For<IGestorConcurrenciaListaEspera>());
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
            var tieneSub = Request.Headers.TryGetValue(HeaderSub, out var valoresSub);
            var tieneRol = Request.Headers.TryGetValue(HeaderRol, out var valoresRol);
            var tieneScope = Request.Headers.TryGetValue(HeaderScope, out var valoresScope);
            var tienePermiso = Request.Headers.TryGetValue(HeaderPermiso, out var valoresPermiso);

            if (!tieneSub && !tieneRol && !tieneScope && !tienePermiso)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>();

            if (tieneSub)
            {
                claims.Add(new Claim("sub", valoresSub.FirstOrDefault() ?? string.Empty));
            }

            if (tieneRol)
            {
                foreach (var rol in valoresRol.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("roles", rol!));
                }
            }

            if (tieneScope)
            {
                foreach (var scope in valoresScope.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("scope", scope!));
                }
            }

            if (tienePermiso)
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
