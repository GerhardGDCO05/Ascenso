using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.Comandos;

public sealed class ConfirmarRankingConvocatoriaCommandHandlerTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000008001");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000008002");
    private static readonly Guid _postulacion1Id = Guid.Parse("01968000-0000-7000-8000-000000008003");
    private static readonly Guid _postulacion2Id = Guid.Parse("01968000-0000-7000-8000-000000008004");
    private static readonly Guid _postulacion3Id = Guid.Parse("01968000-0000-7000-8000-000000008005");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000008006");
    private static readonly DateTimeOffset _inicioRevision = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _cierreRevision = new(2026, 11, 20, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahoraCerrado = new(2026, 11, 26, 14, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IRankingConvocatoriaRepository _rankingRepository = Substitute.For<IRankingConvocatoriaRepository>();
    private readonly IConsultaEstadoEvaluacionesConvocatoriaRepository _consultaEstadoEvaluaciones =
        Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>();
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();
    private readonly IDisponibilidadRankingClient _disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
    private readonly IProveedorMontoEstimadoBeca _proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConfirmarRankingConvocatoriaCommandValidator _validator = new();
    private readonly TimeProvider _timeProvider;

    public ConfirmarRankingConvocatoriaCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);
        _timeProvider = new RelojFijo(_ahoraCerrado);
    }

    private ConfirmarRankingConvocatoriaCommandHandler CrearHandler()
        => new(
            _usuarioActual,
            _rankingRepository,
            _consultaEstadoEvaluaciones,
            _convocatoriaClient,
            _disponibilidadClient,
            _proveedorMonto,
            _unitOfWork,
            _validator,
            _timeProvider);

    [Fact]
    public async Task Debe_RechazarConValidationException_Cuando_ConvocatoriaIdEsVacio()
    {
        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(Guid.Empty)))
            .Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().OnlyContain(e => e.ErrorCode == RankingInvalidoException.CodigoError);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConNoAutenticadoException_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);
        var handler = CrearHandler();

        await handler
            .Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<NoAutenticadoException>();

        await _rankingRepository.DidNotReceiveWithAnyArgs()
            .ListarParaConfirmarPorConvocatoriaAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarConRankingSinResultados_Cuando_NoExistenResultadosConsolidados()
    {
        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<RankingSinResultadosConsolidadosException>();

        ex.Which.Codigo.Should().Be("RANKING_SIN_RESULTADOS");
        ex.Which.EstadoHttp.Should().Be(422);
    }

    [Fact]
    public async Task Debe_RechazarConRankingYaConfirmado_Cuando_AlgunResultadoEstaConfirmado()
    {
        var r1 = CrearResultado(_postulacion1Id, 95.00m);
        r1.ConfirmarRanking(1, 1500.00m, ClasificacionRanking.DentroDeCupo, _ahoraCerrado.AddHours(-1), _coordinadorId);

        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1]);

        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<RankingYaConfirmadoException>();

        ex.Which.Codigo.Should().Be("RANKING_YA_CONFIRMADO");
        ex.Which.EstadoHttp.Should().Be(409);
        await _consultaEstadoEvaluaciones.DidNotReceiveWithAnyArgs()
            .ExistenEvaluacionesPendientesOSinConsolidarAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarConEvaluacionesPendientes_Cuando_ExistenEvaluacionesSinCompletarOConsolidar()
    {
        var r1 = CrearResultado(_postulacion1Id, 90.00m);
        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1]);
        _consultaEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<EvaluacionesPendientesParaConsolidarException>();

        ex.Which.Codigo.Should().Be("EVALUACIONES_PENDIENTES");
        ex.Which.EstadoHttp.Should().Be(409);
        await _convocatoriaClient.DidNotReceiveWithAnyArgs().ObtenerVentanaRevisionAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarSegunVentanaRevision_Cuando_ConvocatoriaInexistenteCanceladaNoIniciadaAbiertaOCaida()
    {
        var r1 = CrearResultado(_postulacion1Id, 90.00m);
        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1]);
        _consultaEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CrearHandler();

        // 404 Convocatoria no encontrada
        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns((VentanaRevisionConvocatoria?)null);
        await handler.Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<ConvocatoriaNoEncontradaException>();

        // 409 Cancelada
        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Cancelada, _inicioRevision, _cierreRevision));
        await handler.Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<ConvocatoriaCanceladaException>();

        // 409 Periodo no iniciado (Borrador o ahora < inicio)
        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Publicada, _ahoraCerrado.AddDays(1), _ahoraCerrado.AddDays(10)));
        await handler.Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<PeriodoRevisionNoIniciadoException>();

        // 409 Periodo abierto
        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Publicada, _ahoraCerrado.AddDays(-1), _ahoraCerrado.AddDays(5)));
        await handler.Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<PeriodoRevisionAbiertoException>();

        // 503 Dependencia no disponible
        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DependenciaNoDisponibleException());
        await handler.Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_FallarCerrado_Cuando_DisponibilidadOMontoFallan()
    {
        var r1 = CrearResultado(_postulacion1Id, 92.00m);
        ConfigurarPrecondicionesBase([r1]);

        var handler = CrearHandler();

        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns((DisponibilidadProgramaRanking?)null);
        await handler.Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<ConvocatoriaNoEncontradaException>();

        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProgramaBecaNoEncontradoException());
        await handler.Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<ProgramaBecaNoEncontradoException>();

        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(CrearDisponibilidad(cupos: 2, presupuesto: 5000.00m));
        _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                _convocatoriaId, _programaBecaId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DependenciaNoDisponibleException());
        await handler.Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<DependenciaNoDisponibleException>();

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_BloquearConEmpatesSinResolver_Cuando_HayEmpateEnLaLineaDeCorte()
    {
        var r1 = CrearResultado(_postulacion1Id, 95.00m);
        var r2 = CrearResultado(_postulacion2Id, 88.00m);
        var r3 = CrearResultado(_postulacion3Id, 88.00m);
        ConfigurarPrecondicionesBase([r1, r2, r3]);

        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m));
        _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                _convocatoriaId, _programaBecaId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [_postulacion1Id] = 1000.00m,
                [_postulacion2Id] = 1000.00m,
                [_postulacion3Id] = 1000.00m
            });

        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<EmpatesSinResolverEnCorteException>();

        ex.Which.Codigo.Should().Be("EMPATES_SIN_RESOLVER");
        ex.Which.EstadoHttp.Should().Be(422);
        r1.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Provisional);
        r2.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Provisional);
        r3.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Provisional);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_ExigirAcuseAnteDiscrepanciasEnCorteYPermitirConfirmarCuandoSeAcusa()
    {
        var r1 = CrearResultado(_postulacion1Id, 95.00m, alerta: true);
        var r2 = CrearResultado(_postulacion2Id, 85.00m);
        ConfigurarPrecondicionesBase([r1, r2]);

        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(CrearDisponibilidad(cupos: 1, presupuesto: 10000.00m));
        _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                _convocatoriaId, _programaBecaId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [_postulacion1Id] = 1500.00m,
                [_postulacion2Id] = 1500.00m
            });

        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId, ConfirmarConAdvertenciaDiscrepancias: false)))
            .Should().ThrowAsync<DiscrepanciasSinRevisarEnCorteException>();

        ex.Which.Codigo.Should().Be("DISCREPANCIAS_SIN_REVISAR_EN_CORTE");
        ex.Which.EstadoHttp.Should().Be(422);
        r1.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Provisional);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);

        var dto = await handler.HandleAsync(
            new ConfirmarRankingConvocatoriaCommand(_convocatoriaId, ConfirmarConAdvertenciaDiscrepancias: true));

        dto.EstadoRanking.Should().Be("CONFIRMADO");
        dto.TieneAdvertenciaDiscrepanciasEnCorte.Should().BeTrue();
        r1.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Confirmado);
        r2.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Confirmado);
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_ConfirmarAtomicamenteConUnUnicoGuardarCambiosYPropagarCancellationToken_Cuando_PrecondicionesSeCumplen()
    {
        using var cts = new CancellationTokenSource();
        var r1 = CrearResultado(_postulacion1Id, 95.00m);
        var r2 = CrearResultado(_postulacion2Id, 84.00m);
        var r3 = CrearResultado(_postulacion3Id, 72.00m);

        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, cts.Token)
            .Returns([r3, r1, r2]);
        _consultaEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, cts.Token)
            .Returns(false);
        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, cts.Token)
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Cerrada, _inicioRevision, _cierreRevision));
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, cts.Token)
            .Returns(CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m));
        _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                _convocatoriaId,
                _programaBecaId,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 3),
                cts.Token)
            .Returns(new Dictionary<Guid, decimal>
            {
                [_postulacion1Id] = 1800.00m,
                [_postulacion2Id] = 1600.00m,
                [_postulacion3Id] = 1200.00m
            });

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId), cts.Token);

        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.EstadoRanking.Should().Be("CONFIRMADO");
        dto.FechaConfirmacionRanking.Should().Be(_ahoraCerrado);
        dto.ConfirmadoPorUsuarioId.Should().Be(_coordinadorId);
        dto.Disponibilidad.Should().NotBeNull();
        dto.TotalPostulantes.Should().Be(3);
        dto.TotalDentroDeCupo.Should().Be(2);
        dto.TotalListaDeEspera.Should().Be(1);
        dto.MontoTotalDentroDeCupo.Should().Be(3400.00m);
        dto.Elementos.Items.Should().HaveCount(3);

        r1.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Confirmado);
        r1.PosicionRanking.Should().Be(1);
        r1.MontoEstimadoBeca.Should().Be(1800.00m);
        r1.Clasificacion.Should().Be(ClasificacionRanking.DentroDeCupo);
        r1.EsAdjudicable.Should().BeTrue();
        r1.ConfirmadoPorUsuarioId.Should().Be(_coordinadorId);
        r1.FechaConfirmacionRanking.Should().Be(_ahoraCerrado);

        r2.PosicionRanking.Should().Be(2);
        r2.Clasificacion.Should().Be(ClasificacionRanking.DentroDeCupo);
        r2.EsAdjudicable.Should().BeTrue();

        r3.PosicionRanking.Should().Be(3);
        r3.Clasificacion.Should().Be(ClasificacionRanking.ListaDeEspera);
        r3.EsAdjudicable.Should().BeFalse();

        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    private void ConfigurarPrecondicionesBase(IReadOnlyList<ResultadoConsolidadoRanking> resultados)
    {
        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(resultados);
        _consultaEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);
        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Cerrada, _inicioRevision, _cierreRevision));
    }

    private static DisponibilidadProgramaRanking CrearDisponibilidad(int cupos, decimal presupuesto)
        => new(_convocatoriaId, _programaBecaId, cupos, cupos, presupuesto, 0.00m, "USD");

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid postulacionId,
        decimal puntaje,
        bool alerta = false)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: alerta ? 24.00m : 5.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: alerta);

        return ResultadoConsolidadoRanking.Crear(calculo, _cierreRevision.AddHours(-2), UsuarioSistema.Id);
    }

    private sealed class RelojFijo(DateTimeOffset ahoraUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahoraUtc;
    }
}
