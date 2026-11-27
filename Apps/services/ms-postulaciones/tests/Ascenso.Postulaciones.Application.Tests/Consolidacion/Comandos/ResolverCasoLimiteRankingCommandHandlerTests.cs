using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ResolverCasoLimiteRanking;
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

public sealed class ResolverCasoLimiteRankingCommandHandlerTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000010001");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000010002");
    private static readonly Guid _postulacion1Id = Guid.Parse("01968000-0000-7000-8000-000000010003");
    private static readonly Guid _postulacion2Id = Guid.Parse("01968000-0000-7000-8000-000000010004");
    private static readonly Guid _postulacion3Id = Guid.Parse("01968000-0000-7000-8000-000000010005");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000010006");
    private static readonly Guid _jefaturaId = Guid.Parse("01968000-0000-7000-8000-000000010007");
    private static readonly Guid _miembroRegularId = Guid.Parse("01968000-0000-7000-8000-000000010008");
    private static readonly DateTimeOffset _inicioRevision = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _cierreRevision = new(2026, 11, 20, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahoraCerrado = new(2026, 11, 26, 14, 0, 0, TimeSpan.Zero);

    private const string JustificacionValida = "Priorización por mayor vulnerabilidad socioeconómica acreditada en comité.";

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IRolesUsuarioActual _rolesUsuarioActual = Substitute.For<IRolesUsuarioActual>();
    private readonly IConsultaJefaturaComiteRepository _consultaJefatura = Substitute.For<IConsultaJefaturaComiteRepository>();
    private readonly IVerificadorConflictoInteresDecisor _verificadorConflicto = Substitute.For<IVerificadorConflictoInteresDecisor>();
    private readonly IRankingConvocatoriaRepository _rankingRepository = Substitute.For<IRankingConvocatoriaRepository>();
    private readonly IConsultaEstadoEvaluacionesConvocatoriaRepository _consultaEstadoEvaluaciones =
        Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>();
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();
    private readonly IDisponibilidadRankingClient _disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
    private readonly IProveedorMontoEstimadoBeca _proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ResolverCasoLimiteRankingCommandValidator _validator = new();
    private readonly TimeProvider _timeProvider;

    public ResolverCasoLimiteRankingCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);
        _rolesUsuarioActual.TieneRol(ResolverCasoLimiteRankingCommandHandler.RolCoordinadorPrograma).Returns(true);
        _verificadorConflicto
            .TieneConflictoEnPostulacionesAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _timeProvider = new RelojFijo(_ahoraCerrado);
    }

    private ResolverCasoLimiteRankingCommandHandler CrearHandler()
        => new(
            _usuarioActual,
            _rolesUsuarioActual,
            _consultaJefatura,
            _verificadorConflicto,
            _rankingRepository,
            _consultaEstadoEvaluaciones,
            _convocatoriaClient,
            _disponibilidadClient,
            _proveedorMonto,
            _unitOfWork,
            _validator,
            _timeProvider);

    [Fact]
    public async Task Debe_RechazarConValidationException_Cuando_EntradasDelComandoSonInvalidas()
    {
        var handler = CrearHandler();

        var exConv = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(Guid.Empty, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<ValidationException>();
        exConv.Which.Errors.Should().Contain(e => e.ErrorCode == RankingInvalidoException.CodigoError);

        var exVacia = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [], JustificacionValida)))
            .Should().ThrowAsync<ValidationException>();
        exVacia.Which.Errors.Should().Contain(e => e.ErrorCode == SeleccionCasoLimiteInvalidaException.CodigoError);

        var exGuidVacio = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [Guid.Empty], JustificacionValida)))
            .Should().ThrowAsync<ValidationException>();
        exGuidVacio.Which.Errors.Should().Contain(e => e.ErrorCode == SeleccionCasoLimiteInvalidaException.CodigoError);

        var exDuplicada = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id, _postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<ValidationException>();
        exDuplicada.Which.Errors.Should().Contain(e => e.ErrorCode == SeleccionCasoLimiteInvalidaException.CodigoError);

        var exJustCorta = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], "   corta   ")))
            .Should().ThrowAsync<ValidationException>();
        exJustCorta.Which.Errors.Should().Contain(e => e.ErrorCode == JustificacionCasoLimiteInvalidaException.CodigoError);

        var exJustLarga = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], new string('a', 1001))))
            .Should().ThrowAsync<ValidationException>();
        exJustLarga.Which.Errors.Should().Contain(e => e.ErrorCode == JustificacionCasoLimiteInvalidaException.CodigoError);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConNoAutenticadoException_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);
        var handler = CrearHandler();

        await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion2Id], JustificacionValida)))
            .Should().ThrowAsync<NoAutenticadoException>();

        await _consultaJefatura.DidNotReceiveWithAnyArgs()
            .EsJefaturaActivaEnConvocatoriaAsync(default, default, default);
        await _rankingRepository.DidNotReceiveWithAnyArgs()
            .ListarParaConfirmarPorConvocatoriaAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon403PermisoDenegadoSinConsultarMsPrg_Cuando_UsuarioNoEsCoordinadorNiJefaturaActiva()
    {
        _usuarioActual.UsuarioId.Returns(_miembroRegularId);
        _rolesUsuarioActual.TieneRol(ResolverCasoLimiteRankingCommandHandler.RolCoordinadorPrograma).Returns(false);
        _consultaJefatura.EsJefaturaActivaEnConvocatoriaAsync(_convocatoriaId, _miembroRegularId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion2Id], JustificacionValida)))
            .Should().ThrowAsync<PermisoDenegadoResolucionCasoLimiteException>();

        ex.Which.Codigo.Should().Be("PERMISO_DENEGADO");
        ex.Which.EstadoHttp.Should().Be(403);

        await _rankingRepository.DidNotReceiveWithAnyArgs().ListarParaConfirmarPorConvocatoriaAsync(default, default);
        await _convocatoriaClient.DidNotReceiveWithAnyArgs().ObtenerVentanaRevisionAsync(default, default);
        await _disponibilidadClient.DidNotReceiveWithAnyArgs().ObtenerDisponibilidadPorConvocatoriaAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon403ConflictoInteresDecisor_Cuando_VerificadorReportaConflicto()
    {
        var (r1, r2, r3, montos) = PrepararEmpatePorCupo();
        ConfigurarPrecondicionesBase([r1, r2, r3], CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m), montos);
        _verificadorConflicto
            .TieneConflictoEnPostulacionesAsync(
                _convocatoriaId,
                _coordinadorId,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { _postulacion2Id, _postulacion3Id })),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion3Id], JustificacionValida)))
            .Should().ThrowAsync<ConflictoInteresDecisorCasoLimiteException>();

        ex.Which.Codigo.Should().Be("CONFLICTO_INTERES_DECISOR");
        ex.Which.EstadoHttp.Should().Be(403);
        r2.TieneDecisionCasoLimite.Should().BeFalse();
        r3.TieneDecisionCasoLimite.Should().BeFalse();
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarPrecondicionesDeRankingEvaluacionesYVentanaRevision()
    {
        var handler = CrearHandler();

        // 422 RANKING_SIN_RESULTADOS
        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([]);
        var exSinResultados = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<RankingSinResultadosConsolidadosException>();
        exSinResultados.Which.EstadoHttp.Should().Be(422);

        // 409 RANKING_YA_CONFIRMADO
        var rConfirmado = CrearResultado(_postulacion1Id, 95.00m);
        rConfirmado.ConfirmarRanking(1, 1500.00m, ClasificacionRanking.DentroDeCupo, _ahoraCerrado.AddHours(-1), _coordinadorId);
        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([rConfirmado]);
        var exConfirmado = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<RankingYaConfirmadoException>();
        exConfirmado.Which.EstadoHttp.Should().Be(409);

        // 409 EVALUACIONES_PENDIENTES
        var rProvisional = CrearResultado(_postulacion1Id, 95.00m);
        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([rProvisional]);
        _consultaEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(true);
        var exPendientes = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<EvaluacionesPendientesParaConsolidarException>();
        exPendientes.Which.EstadoHttp.Should().Be(409);

        // Ventana revisión: 404, 409 Cancelada, 409 NoIniciado, 409 Abierto, 503
        _consultaEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);

        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns((VentanaRevisionConvocatoria?)null);
        await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<ConvocatoriaNoEncontradaException>();

        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Cancelada, _inicioRevision, _cierreRevision));
        await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<ConvocatoriaCanceladaException>();

        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Publicada, _ahoraCerrado.AddDays(1), _ahoraCerrado.AddDays(5)));
        await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<PeriodoRevisionNoIniciadoException>();

        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Publicada, _inicioRevision, _ahoraCerrado.AddDays(2)));
        await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<PeriodoRevisionAbiertoException>();

        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DependenciaNoDisponibleException());
        await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_FallarCerradoAnteMsPrg_Cuando_DisponibilidadEsNulaONoDisponible()
    {
        var r1 = CrearResultado(_postulacion1Id, 95.00m);
        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1]);
        _consultaEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);
        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Cerrada, _inicioRevision, _cierreRevision));

        var handler = CrearHandler();

        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns((DisponibilidadProgramaRanking?)null);
        await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<ConvocatoriaNoEncontradaException>();

        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DependenciaNoDisponibleException());
        await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    [Fact]
    public async Task Debe_Distinguir422CasoLimiteNoDetectadoDe409CasoLimiteYaResuelto_YRechazarSeleccionInvalidaOExcesiva()
    {
        var (r1, r2, r3, montos) = PrepararEmpatePorCupo();
        var handler = CrearHandler();

        // 422 CASO_LIMITE_NO_DETECTADO (todos caben)
        ConfigurarPrecondicionesBase([r1, r2, r3], CrearDisponibilidad(cupos: 5, presupuesto: 10000.00m), montos);
        var exNoDetectado = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion2Id], JustificacionValida)))
            .Should().ThrowAsync<CasoLimiteNoDetectadoException>();
        exNoDetectado.Which.Codigo.Should().Be("CASO_LIMITE_NO_DETECTADO");
        exNoDetectado.Which.EstadoHttp.Should().Be(422);

        // Con empate activo (cupos = 2): 422 SELECCION_CASO_LIMITE_INVALIDA (r1 fuera del grupo)
        ConfigurarPrecondicionesBase([r1, r2, r3], CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m), montos);
        var exSeleccionInvalida = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion1Id], JustificacionValida)))
            .Should().ThrowAsync<SeleccionCasoLimiteInvalidaException>();
        exSeleccionInvalida.Which.Codigo.Should().Be("SELECCION_CASO_LIMITE_INVALIDA");

        // 422 SELECCION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD (selecciona 2 para 1 cupo)
        var exExcede = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion2Id, _postulacion3Id], JustificacionValida)))
            .Should().ThrowAsync<SeleccionCasoLimiteExcedeDisponibilidadException>();
        exExcede.Which.Codigo.Should().Be("SELECCION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD");

        // Resuelve válidamente y luego intenta sobrescribir -> 409 CASO_LIMITE_YA_RESUELTO
        await handler.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion3Id], JustificacionValida));

        var exYaResuelto = await handler
            .Invoking(h => h.HandleAsync(new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion2Id], JustificacionValida)))
            .Should().ThrowAsync<CasoLimiteYaResueltoException>();
        exYaResuelto.Which.Codigo.Should().Be("CASO_LIMITE_YA_RESUELTO");
        exYaResuelto.Which.EstadoHttp.Should().Be(409);
    }

    [Fact]
    public async Task Debe_ResolverEmpatePorCupoPorCoordinadorSinConsultarJefaturaYPropagarCancellationToken()
    {
        var (r1, r2, r3, montos) = PrepararEmpatePorCupo();
        ConfigurarPrecondicionesBase([r1, r2, r3], CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m), montos);

        using var cts = new CancellationTokenSource();
        var handler = CrearHandler();

        var resultado = await handler.HandleAsync(
            new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion3Id], $"  {JustificacionValida}  "),
            cts.Token);

        resultado.EstadoRanking.Should().Be("PROVISIONAL");
        resultado.TieneEmpateEnLineaDeCorte.Should().BeFalse();
        resultado.TieneCasoLimiteActivo.Should().BeFalse();
        resultado.TieneCasoLimiteResuelto.Should().BeTrue();
        resultado.TotalDentroDeCupo.Should().Be(2);
        resultado.TotalListaDeEspera.Should().Be(1);

        resultado.Elementos.Items[0].PostulacionId.Should().Be(_postulacion1Id);
        resultado.Elementos.Items[1].PostulacionId.Should().Be(_postulacion3Id);
        resultado.Elementos.Items[1].Clasificacion.Should().Be("DENTRO_DE_CUPO");
        resultado.Elementos.Items[1].SeleccionadoEnCasoLimite.Should().BeTrue();
        resultado.Elementos.Items[2].PostulacionId.Should().Be(_postulacion2Id);
        resultado.Elementos.Items[2].Clasificacion.Should().Be("LISTA_DE_ESPERA");
        resultado.Elementos.Items[2].SeleccionadoEnCasoLimite.Should().BeFalse();

        r3.TipoCasoLimite.Should().Be(TipoCasoLimite.EmpatePorCupo);
        r3.DecididoPorUsuarioId.Should().Be(_coordinadorId);
        r3.FechaDecisionCasoLimite.Should().Be(_ahoraCerrado);
        r3.JustificacionDesempateCasoLimite.Should().Be(JustificacionValida);
        r2.TipoCasoLimite.Should().Be(TipoCasoLimite.EmpatePorCupo);
        r2.SeleccionadoEnCasoLimite.Should().BeFalse();

        await _consultaJefatura.DidNotReceiveWithAnyArgs().EsJefaturaActivaEnConvocatoriaAsync(default, default, default);
        await _rankingRepository.Received(1).ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, cts.Token);
        await _consultaEstadoEvaluaciones.Received(1).ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, cts.Token);
        await _convocatoriaClient.Received(1).ObtenerVentanaRevisionAsync(_convocatoriaId, cts.Token);
        await _disponibilidadClient.Received(1).ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, cts.Token);
        await _verificadorConflicto.Received(1).TieneConflictoEnPostulacionesAsync(
            _convocatoriaId,
            _coordinadorId,
            Arg.Any<IReadOnlyCollection<Guid>>(),
            cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_ResolverEmpatePorPresupuestoPorJefaturaActivaDelComite()
    {
        _usuarioActual.UsuarioId.Returns(_jefaturaId);
        _rolesUsuarioActual.TieneRol(ResolverCasoLimiteRankingCommandHandler.RolCoordinadorPrograma).Returns(false);
        _consultaJefatura.EsJefaturaActivaEnConvocatoriaAsync(_convocatoriaId, _jefaturaId, Arg.Any<CancellationToken>())
            .Returns(true);

        var r1 = CrearResultado(_postulacion1Id, 90.00m);
        var r2 = CrearResultado(_postulacion2Id, 90.00m, minutosDesfase: 1);
        var montos = new Dictionary<Guid, decimal>
        {
            [_postulacion1Id] = 2000.00m,
            [_postulacion2Id] = 2000.00m
        };

        ConfigurarPrecondicionesBase([r1, r2], CrearDisponibilidad(cupos: 5, presupuesto: 3000.00m), montos);

        var handler = CrearHandler();
        var resultado = await handler.HandleAsync(
            new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion2Id], JustificacionValida));

        resultado.TieneEmpateEnLineaDeCorte.Should().BeFalse();
        resultado.TieneCasoLimiteResuelto.Should().BeTrue();
        resultado.Elementos.Items[0].PostulacionId.Should().Be(_postulacion2Id);
        resultado.Elementos.Items[0].TipoCasoLimite.Should().Be("EMPATE_POR_PRESUPUESTO");
        resultado.Elementos.Items[0].DecididoPorUsuarioId.Should().Be(_jefaturaId);

        await _consultaJefatura.Received(1).EsJefaturaActivaEnConvocatoriaAsync(_convocatoriaId, _jefaturaId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_ResolverCasoLimitePresupuestarioSinEmpateConPersistenciaAtomica()
    {
        var r1 = CrearResultado(_postulacion1Id, 94.00m);
        var r2 = CrearResultado(_postulacion2Id, 89.00m, minutosDesfase: 1);
        var r3 = CrearResultado(_postulacion3Id, 80.00m, minutosDesfase: 2);
        var montos = new Dictionary<Guid, decimal>
        {
            [_postulacion1Id] = 2000.00m,
            [_postulacion2Id] = 2000.00m,
            [_postulacion3Id] = 400.00m
        };

        ConfigurarPrecondicionesBase([r1, r2, r3], CrearDisponibilidad(cupos: 5, presupuesto: 2500.00m), montos);

        var handler = CrearHandler();
        var resultado = await handler.HandleAsync(
            new ResolverCasoLimiteRankingCommand(_convocatoriaId, [_postulacion3Id], JustificacionValida));

        resultado.TieneEmpateEnLineaDeCorte.Should().BeFalse();
        resultado.TieneCasoLimiteActivo.Should().BeFalse();
        resultado.TieneCasoLimiteResuelto.Should().BeTrue();
        resultado.TotalDentroDeCupo.Should().Be(2);
        resultado.TotalListaDeEspera.Should().Be(1);
        resultado.MontoTotalDentroDeCupo.Should().Be(2400.00m);

        resultado.Elementos.Items[1].PostulacionId.Should().Be(_postulacion2Id);
        resultado.Elementos.Items[1].Clasificacion.Should().Be("LISTA_DE_ESPERA");
        resultado.Elementos.Items[2].PostulacionId.Should().Be(_postulacion3Id);
        resultado.Elementos.Items[2].Clasificacion.Should().Be("DENTRO_DE_CUPO");
        resultado.Elementos.Items[2].TipoCasoLimite.Should().Be("CASO_LIMITE_PRESUPUESTARIO");

        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    private (ResultadoConsolidadoRanking R1, ResultadoConsolidadoRanking R2, ResultadoConsolidadoRanking R3, Dictionary<Guid, decimal> Montos)
        PrepararEmpatePorCupo()
    {
        var r1 = CrearResultado(_postulacion1Id, 95.00m);
        var r2 = CrearResultado(_postulacion2Id, 88.00m, minutosDesfase: 1);
        var r3 = CrearResultado(_postulacion3Id, 88.00m, minutosDesfase: 2);
        var montos = new Dictionary<Guid, decimal>
        {
            [_postulacion1Id] = 1000.00m,
            [_postulacion2Id] = 1000.00m,
            [_postulacion3Id] = 1000.00m
        };

        return (r1, r2, r3, montos);
    }

    private void ConfigurarPrecondicionesBase(
        IReadOnlyList<ResultadoConsolidadoRanking> resultados,
        DisponibilidadProgramaRanking disponibilidad,
        IReadOnlyDictionary<Guid, decimal> montos)
    {
        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(resultados);
        _consultaEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);
        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Cerrada, _inicioRevision, _cierreRevision));
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(disponibilidad);
        _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                _convocatoriaId,
                disponibilidad.ProgramaBecaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(montos);
    }

    private static DisponibilidadProgramaRanking CrearDisponibilidad(int cupos, decimal presupuesto)
        => new(_convocatoriaId, _programaBecaId, cupos, cupos, presupuesto, 0.00m, "USD");

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid postulacionId,
        decimal puntaje,
        int minutosDesfase = 0)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            postulacionId,
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
            _cierreRevision.AddHours(-2).AddMinutes(minutosDesfase),
            UsuarioSistema.Id);
    }

    private sealed class RelojFijo(DateTimeOffset ahoraUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahoraUtc;
    }
}
