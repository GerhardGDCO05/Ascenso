using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
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

public sealed class ResolucionCasosLimiteControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string RolCoordinador = "COORDINADOR_PROGRAMA";
    private const string RolMiembroComite = "MIEMBRO_COMITE";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public ResolucionCasosLimiteControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid convocatoriaId,
        decimal puntaje,
        DateTimeOffset fechaConsolidacion)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId: convocatoriaId,
            postulacionId: Guid.NewGuid(),
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: 4.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        return ResultadoConsolidadoRanking.Crear(
            calculo,
            fechaConsolidacion,
            Guid.NewGuid());
    }

    [Fact]
    public async Task Debe_Retornar200OkConRankingActualizado_Cuando_CoordinadorResuelveEmpatePorCupo()
    {
        var ahora = DateTimeOffset.UtcNow;
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();
        var r1 = CrearResultado(convocatoriaId, 95.00m, ahora.AddMinutes(-30));
        var r2 = CrearResultado(convocatoriaId, 88.00m, ahora.AddMinutes(-25));
        var r3 = CrearResultado(convocatoriaId, 88.00m, ahora.AddMinutes(-20));

        var repoRanking = Substitute.For<IRankingConvocatoriaRepository>();
        repoRanking.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r2, r3]);

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
                presupuestoDisponibleBruto: 10000.00m,
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
                [r1.PostulacionId] = 1500.00m,
                [r2.PostulacionId] = 1500.00m,
                [r3.PostulacionId] = 1500.00m
            });

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(
            repoRanking: repoRanking,
            repoEstadoEvaluaciones: repoEstado,
            convocatoriaClient: convocatoriaClient,
            disponibilidadClient: disponibilidadClient,
            proveedorMonto: proveedorMonto,
            uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { r3.PostulacionId },
                justificacion = "Mayor puntaje en producción científica acreditada conforme al acta de comité."
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<RankingConvocatoriaDto>();
        dto.Should().NotBeNull();
        dto!.TieneEmpateEnLineaDeCorte.Should().BeFalse();
        dto.TieneCasoLimiteActivo.Should().BeFalse();
        dto.TieneCasoLimiteResuelto.Should().BeTrue();
        dto.TotalDentroDeCupo.Should().Be(2);
        dto.TotalListaDeEspera.Should().Be(1);

        var ganador = dto.Elementos.Items.Single(e => e.PostulacionId == r3.PostulacionId);
        ganador.PosicionRanking.Should().Be(2);
        ganador.Clasificacion.Should().Be("DENTRO_DE_CUPO");
        ganador.TipoCasoLimite.Should().Be("EMPATE_POR_CUPO");
        ganador.SeleccionadoEnCasoLimite.Should().BeTrue();
        ganador.DecididoPorUsuarioId.Should().Be(coordinadorId);
        ganador.CasoLimiteResuelto.Should().BeTrue();

        var noSeleccionado = dto.Elementos.Items.Single(e => e.PostulacionId == r2.PostulacionId);
        noSeleccionado.PosicionRanking.Should().Be(3);
        noSeleccionado.Clasificacion.Should().Be("LISTA_DE_ESPERA");
        noSeleccionado.TipoCasoLimite.Should().Be("EMPATE_POR_CUPO");
        noSeleccionado.SeleccionadoEnCasoLimite.Should().BeFalse();
        noSeleccionado.CasoLimiteResuelto.Should().BeTrue();

        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar200Ok_Cuando_JefaturaActivaDelComiteResuelveCasoLimitePresupuestario()
    {
        var ahora = DateTimeOffset.UtcNow;
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var jefaturaId = Guid.NewGuid();
        var r1 = CrearResultado(convocatoriaId, 94.00m, ahora.AddMinutes(-30));
        var r2 = CrearResultado(convocatoriaId, 89.00m, ahora.AddMinutes(-25));
        var r3 = CrearResultado(convocatoriaId, 80.00m, ahora.AddMinutes(-20));

        var repoJefatura = Substitute.For<IConsultaJefaturaComiteRepository>();
        repoJefatura.EsJefaturaActivaEnConvocatoriaAsync(convocatoriaId, jefaturaId, Arg.Any<CancellationToken>())
            .Returns(true);

        var repoRanking = Substitute.For<IRankingConvocatoriaRepository>();
        repoRanking.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r2, r3]);

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
                cuposAsignadosConvocatoria: 5,
                cuposDisponiblesPrograma: 5,
                presupuestoDisponibleBruto: 2500.00m,
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
                [r1.PostulacionId] = 2000.00m,
                [r2.PostulacionId] = 2000.00m,
                [r3.PostulacionId] = 400.00m
            });

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(
            repoJefatura: repoJefatura,
            repoRanking: repoRanking,
            repoEstadoEvaluaciones: repoEstado,
            convocatoriaClient: convocatoriaClient,
            disponibilidadClient: disponibilidadClient,
            proveedorMonto: proveedorMonto,
            uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, jefaturaId, RolMiembroComite);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { r3.PostulacionId },
                justificacion = "Se asigna saldo presupuestario de $500.00 al postulante cuyo monto cabe íntegramente."
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<RankingConvocatoriaDto>();
        dto.Should().NotBeNull();
        dto!.TieneCasoLimiteResuelto.Should().BeTrue();
        dto.TotalDentroDeCupo.Should().Be(2);
        dto.TotalListaDeEspera.Should().Be(1);

        var elem2 = dto.Elementos.Items.Single(e => e.PostulacionId == r2.PostulacionId);
        elem2.PosicionRanking.Should().Be(2);
        elem2.Clasificacion.Should().Be("LISTA_DE_ESPERA");

        var elem3 = dto.Elementos.Items.Single(e => e.PostulacionId == r3.PostulacionId);
        elem3.PosicionRanking.Should().Be(3);
        elem3.Clasificacion.Should().Be("DENTRO_DE_CUPO");
        elem3.TipoCasoLimite.Should().Be("CASO_LIMITE_PRESUPUESTARIO");
        elem3.DecididoPorUsuarioId.Should().Be(jefaturaId);
    }

    [Fact]
    public async Task Debe_Retornar400403409Y422EnProblemDetails_Cuando_FallanValidacionesOPrecondicionesDeCasoLimite()
    {
        var ahora = DateTimeOffset.UtcNow;
        var convocatoriaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();
        var r1 = CrearResultado(convocatoriaId, 95.00m, ahora.AddMinutes(-30));
        var r2 = CrearResultado(convocatoriaId, 88.00m, ahora.AddMinutes(-25));
        var r3 = CrearResultado(convocatoriaId, 88.00m, ahora.AddMinutes(-20));

        var repoRanking = Substitute.For<IRankingConvocatoriaRepository>();
        var repoEstado = Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>();
        var convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();
        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        var proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
        var verificadorConflicto = Substitute.For<IVerificadorConflictoInteresDecisor>();

        using var fabrica = CrearFabrica(
            verificadorConflicto: verificadorConflicto,
            repoRanking: repoRanking,
            repoEstadoEvaluaciones: repoEstado,
            convocatoriaClient: convocatoriaClient,
            disponibilidadClient: disponibilidadClient,
            proveedorMonto: proveedorMonto);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        // 1. 400 Bad Request por justificación corta o lista vacía
        var resp400 = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = Array.Empty<Guid>(),
                justificacion = "Corta"
            });
        resp400.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Configurar contexto base
        repoRanking.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r2, r3]);
        repoEstado.ExistenEvaluacionesPendientesOSinConsolidarAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);
        convocatoriaClient.ObtenerVentanaRevisionAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                convocatoriaId,
                EstadoConvocatoriaExterna.Cerrada,
                ahora.AddDays(-10),
                ahora.AddMinutes(-5)));
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: 2,
                cuposDisponiblesPrograma: 2,
                presupuestoDisponibleBruto: 10000.00m,
                presupuestoComprometidoRenovaciones: 0.00m,
                moneda: "USD"));
        proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                convocatoriaId,
                programaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [r1.PostulacionId] = 1500.00m,
                [r2.PostulacionId] = 1500.00m,
                [r3.PostulacionId] = 1500.00m
            });

        // 2. 403 CONFLICTO_INTERES_DECISOR
        verificadorConflicto.TieneConflictoEnPostulacionesAsync(
                convocatoriaId,
                coordinadorId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var resp403Conflicto = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { r2.PostulacionId },
                justificacion = "Justificación formal válida de desempate en línea de corte."
            });
        resp403Conflicto.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await VerificarCodigoErrorAsync(resp403Conflicto, ConflictoInteresDecisorCasoLimiteException.CodigoError);

        verificadorConflicto.TieneConflictoEnPostulacionesAsync(
                convocatoriaId,
                coordinadorId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        // 3. 422 SELECCION_CASO_LIMITE_INVALIDA (postulación ajena al grupo afectado)
        var resp422SeleccionInvalida = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { r1.PostulacionId },
                justificacion = "Justificación formal válida de desempate en línea de corte."
            });
        resp422SeleccionInvalida.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(resp422SeleccionInvalida, SeleccionCasoLimiteInvalidaException.CodigoError);

        // 4. 422 SELECCION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD
        var resp422Excede = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { r2.PostulacionId, r3.PostulacionId },
                justificacion = "Justificación formal válida de desempate en línea de corte."
            });
        resp422Excede.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(resp422Excede, SeleccionCasoLimiteExcedeDisponibilidadException.CodigoError);

        // 5. Resolver válidamente y luego intentar sobrescribir -> 409 CASO_LIMITE_YA_RESUELTO
        var respOk = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { r2.PostulacionId },
                justificacion = "Justificación formal válida de desempate en línea de corte."
            });
        respOk.StatusCode.Should().Be(HttpStatusCode.OK);

        var resp409YaResuelto = await cliente.PostAsJsonAsync(
            $"/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver",
            new
            {
                postulacionesSeleccionadasIds = new[] { r3.PostulacionId },
                justificacion = "Intento de sobrescribir la decisión inalterable ya registrada."
            });
        resp409YaResuelto.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(resp409YaResuelto, CasoLimiteYaResueltoException.CodigoError);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IConsultaJefaturaComiteRepository? repoJefatura = null,
        IVerificadorConflictoInteresDecisor? verificadorConflicto = null,
        IRankingConvocatoriaRepository? repoRanking = null,
        IConsultaEstadoEvaluacionesConvocatoriaRepository? repoEstadoEvaluaciones = null,
        IConvocatoriaEvaluacionClient? convocatoriaClient = null,
        IDisponibilidadRankingClient? disponibilidadClient = null,
        IProveedorMontoEstimadoBeca? proveedorMonto = null,
        IUnitOfWork? uow = null)
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
                servicios.AddSingleton(verificadorConflicto ?? Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(disponibilidadClient ?? Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(proveedorMonto ?? Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(convocatoriaClient ?? Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(uow ?? Substitute.For<IUnitOfWork>());
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

    private static HttpClient CrearClienteAutenticado(
        WebApplicationFactory<Program> fabrica,
        Guid usuarioId,
        string rol)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, rol);
        return cliente;
    }

    private static async Task VerificarCodigoErrorAsync(HttpResponseMessage respuesta, string codigoEsperado)
    {
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        documento.RootElement.GetProperty("codigo").GetString().Should().Be(codigoEsperado);
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
