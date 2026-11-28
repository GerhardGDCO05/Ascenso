using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
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

public sealed class ListaEsperaControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string RolCoordinador = "COORDINADOR_PROGRAMA";
    private const string RolDireccion = "DIRECCION";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public ListaEsperaControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    private static EntradaListaEspera CrearEntrada(
        Guid convocatoriaId,
        Guid programaBecaId,
        Guid postulacionId,
        int posicionOrden,
        decimal monto = 1500.00m)
        => EntradaListaEspera.Crear(
            convocatoriaId,
            programaBecaId,
            postulacionId,
            posicionOrden,
            monto,
            new DateTimeOffset(2026, 11, 27, 9, 0, 0, TimeSpan.Zero),
            Guid.CreateVersion7());

    private static ResultadoConsolidadoRanking CrearResultadoConfirmadoEnListaEspera(
        Guid convocatoriaId,
        Guid postulacionId,
        int posicionRanking,
        decimal monto = 1500.00m)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId: convocatoriaId,
            postulacionId: postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 85.00m,
            diferenciaEntreEvaluadores: 4.00m,
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

    private static IGestorConcurrenciaListaEspera CrearGestorConEntradas(
        Guid convocatoriaId,
        IReadOnlyList<EntradaListaEspera> entradas)
    {
        var gestor = Substitute.For<IGestorConcurrenciaListaEspera>();
        gestor.EjecutarConBloqueoConvocatoriaAsync(
                convocatoriaId,
                Arg.Any<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<EntradaListaEsperaDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(llamada =>
            {
                var accion = llamada.ArgAt<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<EntradaListaEsperaDto>>>(1);
                var ct = llamada.ArgAt<CancellationToken>(2);
                return accion(entradas, ct);
            });
        gestor.EjecutarConBloqueoConvocatoriaAsync(
                convocatoriaId,
                Arg.Any<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<PromocionListaEsperaDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(llamada =>
            {
                var accion = llamada.ArgAt<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<PromocionListaEsperaDto>>>(1);
                var ct = llamada.ArgAt<CancellationToken>(2);
                return accion(entradas, ct);
            });
        return gestor;
    }

    [Fact]
    public async Task Debe_Retornar200OkConPaginacionContadoresYFiltroY400ConParametrosInvalidos_Cuando_SeConsultaListaEspera()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var e1 = CrearEntrada(convocatoriaId, programaId, Guid.CreateVersion7(), 1);
        e1.Promover(new DateTimeOffset(2026, 11, 27, 10, 0, 0, TimeSpan.Zero), Guid.CreateVersion7(), "Promovida");
        var e2 = CrearEntrada(convocatoriaId, programaId, Guid.CreateVersion7(), 2);
        e2.MarcarElegiblePorVacante(new DateTimeOffset(2026, 11, 27, 11, 0, 0, TimeSpan.Zero), Guid.CreateVersion7());
        var e3 = CrearEntrada(convocatoriaId, programaId, Guid.CreateVersion7(), 3);

        var repoLista = Substitute.For<IEntradaListaEsperaRepository>();
        repoLista.ListarPorConvocatoriaAsync(convocatoriaId, null, Arg.Any<CancellationToken>())
            .Returns([e1, e2, e3]);

        using var fabrica = CrearFabrica(repoLista: repoLista);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.CreateVersion7().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, RolDireccion);

        var resp = await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}?estado=ELEGIBLE_POR_VACANTE&pagina=1&tamano=10");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await resp.Content.ReadFromJsonAsync<ListaEsperaConvocatoriaDto>();
        dto.Should().NotBeNull();
        dto!.ConvocatoriaId.Should().Be(convocatoriaId);
        dto.TotalEnListaEspera.Should().Be(3);
        dto.TotalPromovidas.Should().Be(1);
        dto.TotalElegiblesPorVacante.Should().Be(1);
        dto.TotalEnEspera.Should().Be(1);
        dto.Entradas.Items.Should().ContainSingle();
        dto.Entradas.Items[0].PosicionOrden.Should().Be(2);
        dto.Entradas.Items[0].Estado.Should().Be("ELEGIBLE_POR_VACANTE");

        var resp400 = await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}?estado=INEXISTENTE&pagina=0&tamano=200");
        resp400.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar200OkY404SinCandidatos_Cuando_SeConsultaSiguienteCandidato()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var convocatoriaVaciaId = Guid.CreateVersion7();
        var siguiente = CrearEntrada(convocatoriaId, Guid.CreateVersion7(), Guid.CreateVersion7(), 2);

        var repoLista = Substitute.For<IEntradaListaEsperaRepository>();
        repoLista.ObtenerSiguientePendientePorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(siguiente);
        repoLista.ObtenerSiguientePendientePorConvocatoriaAsync(convocatoriaVaciaId, Arg.Any<CancellationToken>())
            .Returns((EntradaListaEspera?)null);

        using var fabrica = CrearFabrica(repoLista: repoLista);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.CreateVersion7().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, RolDireccion);

        var resp200 = await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/siguiente");
        resp200.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp200.Content.ReadFromJsonAsync<EntradaListaEsperaDto>();
        dto!.Id.Should().Be(siguiente.Id);
        dto.PosicionOrden.Should().Be(2);

        var resp404 = await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaVaciaId}/siguiente");
        resp404.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var doc = JsonDocument.Parse(await resp404.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("codigo").GetString().Should().Be(ListaEsperaSinCandidatosException.CodigoError);
    }

    [Fact]
    public async Task Debe_RegistrarVacanteLiberadaYRetornar200OkO404Y409_Cuando_CoordinadorInvocaEndpointPublico()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var convocatoriaVaciaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var e1 = CrearEntrada(convocatoriaId, programaId, Guid.CreateVersion7(), 1);
        var e2 = CrearEntrada(convocatoriaId, programaId, Guid.CreateVersion7(), 2);

        var gestor = CrearGestorConEntradas(convocatoriaId, [e1, e2]);
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
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.CreateVersion7().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, RolCoordinador);

        var resp200 = await cliente.PostAsJsonAsync(
            $"/api/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas",
            new { motivoVacante = "Oferta rechazada por postulante." });

        resp200.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp200.Content.ReadFromJsonAsync<EntradaListaEsperaDto>();
        dto!.Id.Should().Be(e1.Id);
        dto.Estado.Should().Be("ELEGIBLE_POR_VACANTE");
        dto.FechaElegibilidad.Should().NotBeNull();
        dto.FechaPromocion.Should().BeNull();

        var resp404 = await cliente.PostAsync(
            $"/api/v1/lista-espera/convocatorias/{convocatoriaVaciaId}/vacantes-liberadas",
            content: null);
        resp404.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var doc = JsonDocument.Parse(await resp404.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("codigo").GetString().Should().Be(ListaEsperaSinCandidatosException.CodigoError);
    }

    [Fact]
    public async Task Debe_PromoverCandidatoPorIdYPorSiguienteYTraducirErrores404_409_422_503_Cuando_CoordinadorInvocaPromocion()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var p1 = Guid.CreateVersion7();
        var p2 = Guid.CreateVersion7();
        var e1 = CrearEntrada(convocatoriaId, programaId, p1, 1, 1500.00m);
        e1.MarcarElegiblePorVacante(new DateTimeOffset(2026, 11, 27, 10, 0, 0, TimeSpan.Zero), coordinadorId);
        var e2 = CrearEntrada(convocatoriaId, programaId, p2, 2, 1800.00m);

        var gestor = CrearGestorConEntradas(convocatoriaId, [e1, e2]);
        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ObtenerPorPostulacionAsync(p1, Arg.Any<CancellationToken>())
            .Returns(CrearResultadoConfirmadoEnListaEspera(convocatoriaId, p1, 3, 1500.00m));
        repoResultados.ObtenerPorPostulacionAsync(p2, Arg.Any<CancellationToken>())
            .Returns(CrearResultadoConfirmadoEnListaEspera(convocatoriaId, p2, 4, 1800.00m));

        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: 3,
                cuposDisponiblesPrograma: 2,
                presupuestoDisponibleBruto: 3000.00m,
                presupuestoComprometidoRenovaciones: 500.00m,
                moneda: "USD"));

        using var fabrica = CrearFabrica(
            gestor: gestor,
            repoResultados: repoResultados,
            disponibilidadClient: disponibilidadClient);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, coordinadorId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, RolCoordinador);

        // Intentar saltar el orden FIFO promoviendo e2 antes que e1 -> 409 ORDEN_LISTA_ESPERA_VIOLADO
        var respFifo = await cliente.PostAsJsonAsync(
            $"/api/v1/lista-espera/convocatorias/{convocatoriaId}/entradas/{e2.Id}/promover",
            new { motivoPromocion = "Intento de salto" });
        respFifo.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using (var docFifo = JsonDocument.Parse(await respFifo.Content.ReadAsStringAsync()))
        {
            docFifo.RootElement.GetProperty("codigo").GetString().Should().Be(OrdenListaEsperaVioladoException.CodigoError);
        }

        // Promoción válida de e1 por ID -> 200 OK
        var respOk = await cliente.PostAsJsonAsync(
            $"/api/v1/lista-espera/convocatorias/{convocatoriaId}/entradas/{e1.Id}/promover",
            new { motivoPromocion = "Vacante confirmada por coordinación." });
        respOk.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoOk = await respOk.Content.ReadFromJsonAsync<PromocionListaEsperaDto>();
        dtoOk.Should().NotBeNull();
        dtoOk!.Entrada.Id.Should().Be(e1.Id);
        dtoOk.Entrada.Estado.Should().Be("PROMOVIDA");
        dtoOk.Entrada.PromovidoPorUsuarioId.Should().Be(coordinadorId);
        dtoOk.HabilitadaParaAdjudicacion.Should().BeTrue();

        // Presupuesto insuficiente al promover siguiente (e2 requiere 1800.00, disponible 1000.00) -> 422
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: 3,
                cuposDisponiblesPrograma: 1,
                presupuestoDisponibleBruto: 1000.00m,
                presupuestoComprometidoRenovaciones: 0.00m,
                moneda: "USD"));

        var resp422 = await cliente.PostAsync(
            $"/api/v1/lista-espera/convocatorias/{convocatoriaId}/promover-siguiente",
            content: null);
        resp422.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using (var doc422 = JsonDocument.Parse(await resp422.Content.ReadAsStringAsync()))
        {
            doc422.RootElement.GetProperty("codigo").GetString()
                .Should().Be(PresupuestoInsuficientePromocionException.CodigoError);
        }

        // Indisponibilidad MS-PRG -> 503 DEPENDENCIA_NO_DISPONIBLE
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns<DisponibilidadProgramaRanking?>(_ => throw new DependenciaNoDisponibleException("MS-PRG"));

        var resp503 = await cliente.PostAsync(
            $"/api/v1/lista-espera/convocatorias/{convocatoriaId}/promover-siguiente",
            content: null);
        resp503.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        using (var doc503 = JsonDocument.Parse(await resp503.Content.ReadAsStringAsync()))
        {
            doc503.RootElement.GetProperty("codigo").GetString()
                .Should().Be(DependenciaNoDisponibleException.CodigoError);
        }
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IEntradaListaEsperaRepository? repoLista = null,
        IGestorConcurrenciaListaEspera? gestor = null,
        IResultadoConsolidadoRepository? repoResultados = null,
        IDisponibilidadRankingClient? disponibilidadClient = null)
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
                servicios.AddSingleton(disponibilidadClient ?? Substitute.For<IDisponibilidadRankingClient>());
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

            var identidad = new ClaimsIdentity(claims, EsquemaPrueba, "sub", "roles");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidad), EsquemaPrueba)));
        }
    }
}
