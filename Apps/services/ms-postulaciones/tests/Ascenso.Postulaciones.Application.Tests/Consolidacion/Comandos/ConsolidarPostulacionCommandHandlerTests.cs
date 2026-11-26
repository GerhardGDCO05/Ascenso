using System.Reflection;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConsolidarPostulacion;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.Comandos;

public sealed class ConsolidarPostulacionCommandHandlerTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000002201");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000002202");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000002203");
    private static readonly Guid _jefaturaId = Guid.Parse("01968000-0000-7000-8000-000000002204");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000002205");
    private static readonly Guid _evaluador1Id = Guid.Parse("01968000-0000-7000-8000-000000002206");
    private static readonly Guid _evaluador2Id = Guid.Parse("01968000-0000-7000-8000-000000002207");
    private static readonly Guid _evaluador3Id = Guid.Parse("01968000-0000-7000-8000-000000002208");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite = new(2026, 12, 10, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IResultadoConsolidadoRepository _resultadoRepository = Substitute.For<IResultadoConsolidadoRepository>();
    private readonly IConsultaDatosConsolidacionRepository _consultaDatosRepository = Substitute.For<IConsultaDatosConsolidacionRepository>();
    private readonly IProveedorParametrosConsolidacion _proveedorParametros = Substitute.For<IProveedorParametrosConsolidacion>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConsolidarPostulacionCommandHandler _handler;

    public ConsolidarPostulacionCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(null));
        _resultadoRepository
            .RecargarParaActualizarPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(null));
        _resultadoRepository
            .AgregarAsync(Arg.Any<ResultadoConsolidadoRanking>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _consultaDatosRepository
            .ListarAsignacionesVigentesPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AsignacionEvaluacion>>([]));
        _consultaDatosRepository
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Evaluacion>>([]));
        _consultaDatosRepository
            .ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(null));

        _proveedorParametros
            .ObtenerParametrosAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ParametrosConsolidacion.PorDefecto));

        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new ConsolidarPostulacionCommandHandler(
            _usuarioActual,
            _resultadoRepository,
            _consultaDatosRepository,
            _proveedorParametros,
            _unitOfWork,
            new ConsolidarPostulacionCommandValidator(),
            new RelojFijo(_ahora));
    }

    [Fact]
    public async Task Debe_FallarValidacion_Cuando_PostulacionIdEsVacio()
    {
        var comando = new ConsolidarPostulacionCommand(Guid.Empty);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        var excepcion = await accion.Should().ThrowAsync<ValidationException>();
        excepcion.Which.Errors.Should().ContainSingle(e => e.ErrorCode == ConsolidacionInvalidaException.CodigoError);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConNoAutenticado_Cuando_UsuarioNoEstaAutenticadoEnModoManual()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(new ConsolidarPostulacionCommand(_postulacionId));

        await accion.Should().ThrowAsync<NoAutenticadoException>();
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_CrearResultadoInicialYPropagarCancellationToken_Cuando_ConsolidacionManualEsExitosa()
    {
        using var cts = new CancellationTokenSource();
        var (asignaciones, evaluaciones) = CrearEscenarioDosEvaluadores(80.00m, 90.00m);
        ConfigurarEscenario(asignaciones, evaluaciones);

        ResultadoConsolidadoRanking? agregado = null;
        _resultadoRepository
            .AgregarAsync(Arg.Do<ResultadoConsolidadoRanking>(r => agregado = r), cts.Token)
            .Returns(Task.CompletedTask);

        var respuesta = await _handler.HandleAsync(new ConsolidarPostulacionCommand(_postulacionId), cts.Token);

        respuesta.EsCreacion.Should().BeTrue();
        respuesta.Resultado.PostulacionId.Should().Be(_postulacionId);
        respuesta.Resultado.ConvocatoriaId.Should().Be(_convocatoriaId);
        respuesta.Resultado.CantidadEvaluacionesValidas.Should().Be(2);
        respuesta.Resultado.PuntajeConsolidado.Should().Be(85.00m);
        respuesta.Resultado.DiferenciaEntreEvaluadores.Should().Be(10.00m);
        respuesta.Resultado.AlertaDiscrepancia.Should().BeFalse();
        respuesta.Resultado.EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
        respuesta.Resultado.FechaConsolidacion.Should().Be(_ahora);

        agregado.Should().NotBeNull();
        agregado!.UsuarioCreacionId.Should().Be(_coordinadorId);

        await _consultaDatosRepository.Received(1)
            .ListarAsignacionesVigentesPorPostulacionAsync(_postulacionId, cts.Token);
        await _consultaDatosRepository.Received(1)
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(_postulacionId, cts.Token);
        await _consultaDatosRepository.Received(1)
            .ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(_postulacionId, cts.Token);
        await _proveedorParametros.Received(1)
            .ObtenerParametrosAsync(_convocatoriaId, evaluaciones[0].RubricaEvaluacionId, cts.Token);
        await _resultadoRepository.Received(1)
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, cts.Token);
        await _resultadoRepository.Received(1)
            .AgregarAsync(agregado, cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RecalcularYGuardar_Cuando_YaExisteResultadoYCambianLasEvaluaciones()
    {
        var calculoInicial = new ResultadoCalculoConsolidacion(
            _convocatoriaId, _postulacionId, false, null, null, 2, 82.50m, 25.00m, 20.00m, true);
        var existente = ResultadoConsolidadoRanking.Crear(calculoInicial, _ahora.AddHours(-2), UsuarioSistema.Id);
        _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(existente));

        var (a1, e1) = CrearEvaluacionCompleta(_evaluador1Id, 70.00m);
        var (a2, e2) = CrearEvaluacionCompleta(_evaluador2Id, 95.00m);
        var (a3, e3) = CrearEvaluacionCompleta(_evaluador3Id, 85.00m);
        ConfigurarEscenario([a1, a2, a3], [e1, e2, e3]);

        var respuesta = await _handler.HandleAsync(new ConsolidarPostulacionCommand(_postulacionId));

        respuesta.EsCreacion.Should().BeFalse();
        respuesta.Resultado.CantidadEvaluacionesValidas.Should().Be(3);
        respuesta.Resultado.PuntajeConsolidado.Should().Be(83.33m);
        respuesta.Resultado.Version.Should().Be(2);

        await _resultadoRepository.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RetornarIdempotenteSinGuardar_Cuando_SnapshotCoincideConElExistente()
    {
        var calculoInicial = new ResultadoCalculoConsolidacion(
            _convocatoriaId, _postulacionId, false, null, null, 2, 85.00m, 10.00m, 20.00m, false);
        var existente = ResultadoConsolidadoRanking.Crear(calculoInicial, _ahora.AddHours(-2), UsuarioSistema.Id);
        _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(existente));

        var (asignaciones, evaluaciones) = CrearEscenarioDosEvaluadores(80.00m, 90.00m);
        ConfigurarEscenario(asignaciones, evaluaciones);

        var respuesta = await _handler.HandleAsync(new ConsolidarPostulacionCommand(_postulacionId));

        respuesta.EsCreacion.Should().BeFalse();
        respuesta.Resultado.Version.Should().Be(1);
        await _resultadoRepository.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_SerNoOpEnModoAutomatico_Cuando_HayAsignacionesPendientesOEvaluacionesInsuficientes()
    {
        var (a1, e1) = CrearEvaluacionCompleta(_evaluador1Id, 80.00m);
        var a2Pendiente = CrearAsignacionEnEstado(_evaluador2Id, EstadoAsignacionEvaluacion.EnProgreso);
        ConfigurarEscenario([a1, a2Pendiente], [e1]);

        var resultadoPendiente = await _handler.IntentarConsolidarAutomaticamenteAsync(_postulacionId);
        resultadoPendiente.Should().BeNull();

        // Ahora sin pendientes pero solo 1 evaluación y sin excepción
        ConfigurarEscenario([a1], [e1]);
        var resultadoInsuficiente = await _handler.IntentarConsolidarAutomaticamenteAsync(_postulacionId);
        resultadoInsuficiente.Should().BeNull();

        await _resultadoRepository.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_ConsolidarAutomaticamenteConUsuarioSistemaYExcepcionEvaluadorUnico_Cuando_ExisteUnaEvaluacionYExcepcion()
    {
        using var cts = new CancellationTokenSource();
        var (a1, e1) = CrearEvaluacionCompleta(_evaluador1Id, 88.50m);
        var excepcion = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            "Único evaluador especializado disponible en la institución.",
            _ahora.AddHours(-1),
            _coordinadorId,
            Guid.CreateVersion7());
        ConfigurarEscenario([a1], [e1], excepcion);

        ResultadoConsolidadoRanking? agregado = null;
        _resultadoRepository
            .AgregarAsync(Arg.Do<ResultadoConsolidadoRanking>(r => agregado = r), cts.Token)
            .Returns(Task.CompletedTask);

        // No requiere usuario autenticado en modo automático
        _usuarioActual.EstaAutenticado.Returns(false);

        var respuesta = await _handler.IntentarConsolidarAutomaticamenteAsync(_postulacionId, cts.Token);

        respuesta.Should().NotBeNull();
        respuesta!.EsCreacion.Should().BeTrue();
        respuesta.Resultado.ExcepcionEvaluadorUnico.Should().BeTrue();
        respuesta.Resultado.JustificacionEvaluadorUnico.Should().Be(excepcion.Justificacion);
        respuesta.Resultado.AutorizadorExcepcionId.Should().Be(_coordinadorId);
        respuesta.Resultado.PuntajeConsolidado.Should().Be(88.50m);
        respuesta.Resultado.DiferenciaEntreEvaluadores.Should().Be(0.00m);
        respuesta.Resultado.AlertaDiscrepancia.Should().BeFalse();

        agregado.Should().NotBeNull();
        agregado!.UsuarioCreacionId.Should().Be(UsuarioSistema.Id);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarCon409EvaluacionesPendientes_Cuando_ModoManualTieneAsignacionSinCompletar()
    {
        var (a1, e1) = CrearEvaluacionCompleta(_evaluador1Id, 80.00m);
        var a2Pendiente = CrearAsignacionEnEstado(_evaluador2Id, EstadoAsignacionEvaluacion.Pendiente);
        ConfigurarEscenario([a1, a2Pendiente], [e1]);

        var accion = () => _handler.HandleAsync(new ConsolidarPostulacionCommand(_postulacionId));

        await accion.Should()
            .ThrowAsync<EvaluacionesPendientesParaConsolidarException>()
            .Where(ex => ex.Codigo == "EVALUACIONES_PENDIENTES" && ex.EstadoHttp == 409);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422EvaluacionesInsuficientes_Cuando_ModoManualNoAlcanzaMinimoSinExcepcion()
    {
        var (a1, e1) = CrearEvaluacionCompleta(_evaluador1Id, 80.00m);
        ConfigurarEscenario([a1], [e1]);

        var accion = () => _handler.HandleAsync(new ConsolidarPostulacionCommand(_postulacionId));

        await accion.Should()
            .ThrowAsync<EvaluadoresInsuficientesException>()
            .Where(ex => ex.Codigo == "EVALUACIONES_INSUFICIENTES" && ex.EstadoHttp == 422);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409RankingYaConfirmado_Cuando_ResultadoExistenteEstaConfirmado()
    {
        var calculoInicial = new ResultadoCalculoConsolidacion(
            _convocatoriaId, _postulacionId, false, null, null, 2, 85.00m, 10.00m, 20.00m, false);
        var existente = ResultadoConsolidadoRanking.Crear(calculoInicial, _ahora.AddHours(-2), UsuarioSistema.Id);
        FijarEstadoRanking(existente, EstadoRanking.Confirmado);

        _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(existente));

        var (asignaciones, evaluaciones) = CrearEscenarioDosEvaluadores(80.00m, 90.00m);
        ConfigurarEscenario(asignaciones, evaluaciones);

        var accion = () => _handler.HandleAsync(new ConsolidarPostulacionCommand(_postulacionId));

        await accion.Should()
            .ThrowAsync<RankingYaConfirmadoException>()
            .Where(ex => ex.Codigo == "RANKING_YA_CONFIRMADO" && ex.EstadoHttp == 409);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_ResolverCarreraC5Idempotentemente_Cuando_DuplicadoCoincideTrasRecarga()
    {
        var (asignaciones, evaluaciones) = CrearEscenarioDosEvaluadores(72.00m, 96.00m);
        ConfigurarEscenario(asignaciones, evaluaciones);

        var calculoConcurrente = new ResultadoCalculoConsolidacion(
            _convocatoriaId, _postulacionId, false, null, null, 2, 84.00m, 24.00m, 20.00m, true);
        var persistidoPorOtroHilo = ResultadoConsolidadoRanking.Crear(calculoConcurrente, _ahora, UsuarioSistema.Id);

        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ResultadoConsolidadoDuplicadoException()));
        _resultadoRepository
            .RecargarParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(persistidoPorOtroHilo));

        var respuesta = await _handler.IntentarConsolidarAutomaticamenteAsync(_postulacionId);

        respuesta.Should().NotBeNull();
        respuesta!.EsCreacion.Should().BeFalse();
        respuesta.Resultado.Id.Should().Be(persistidoPorOtroHilo.Id);
        respuesta.Resultado.PuntajeConsolidado.Should().Be(84.00m);
        respuesta.Resultado.AlertaDiscrepancia.Should().BeTrue();

        await _resultadoRepository.Received(1)
            .RecargarParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_ReintentarUnaVezEnCarreraC5_Cuando_LlegoUnaEvaluacionMasRecienteEntremedio()
    {
        var (a1, e1) = CrearEvaluacionCompleta(_evaluador1Id, 70.00m);
        var (a2, e2) = CrearEvaluacionCompleta(_evaluador2Id, 90.00m);
        var (a3, e3) = CrearEvaluacionCompleta(_evaluador3Id, 80.00m);

        var calculoInicial = new ResultadoCalculoConsolidacion(
            _convocatoriaId, _postulacionId, false, null, null, 2, 80.00m, 20.00m, 20.00m, false);
        var recargado = ResultadoConsolidadoRanking.Crear(calculoInicial, _ahora.AddMinutes(-1), UsuarioSistema.Id);

        _consultaDatosRepository
            .ListarAsignacionesVigentesPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AsignacionEvaluacion>>([a1, a2, a3]));

        // Primera lectura ve 2 evaluaciones, tras conflicto ve 3 evaluaciones
        _consultaDatosRepository
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<IReadOnlyList<Evaluacion>>([e1, e2]),
                Task.FromResult<IReadOnlyList<Evaluacion>>([e1, e2, e3]));

        _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(null));
        _resultadoRepository
            .RecargarParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(recargado));

        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<int>(new ExcepcionConflictoConcurrencia()),
                Task.FromResult(1));

        var respuesta = await _handler.HandleAsync(new ConsolidarPostulacionCommand(_postulacionId));

        respuesta.EsCreacion.Should().BeFalse();
        respuesta.Resultado.CantidadEvaluacionesValidas.Should().Be(3);
        respuesta.Resultado.PuntajeConsolidado.Should().Be(80.00m);
        respuesta.Resultado.Version.Should().Be(2);

        await _resultadoRepository.Received(1)
            .RecargarParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    private void ConfigurarEscenario(
        IReadOnlyList<AsignacionEvaluacion> asignaciones,
        IReadOnlyList<Evaluacion> evaluaciones,
        AutorizacionExcepcionEvaluadorUnico? excepcion = null)
    {
        _consultaDatosRepository
            .ListarAsignacionesVigentesPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(asignaciones));
        _consultaDatosRepository
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(evaluaciones));
        _consultaDatosRepository
            .ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(excepcion));
    }

    private static (IReadOnlyList<AsignacionEvaluacion> Asignaciones, IReadOnlyList<Evaluacion> Evaluaciones)
        CrearEscenarioDosEvaluadores(decimal puntaje1, decimal puntaje2)
    {
        var (a1, e1) = CrearEvaluacionCompleta(_evaluador1Id, puntaje1);
        var (a2, e2) = CrearEvaluacionCompleta(_evaluador2Id, puntaje2);
        return ([a1, a2], [e1, e2]);
    }

    private static (AsignacionEvaluacion Asignacion, Evaluacion Evaluacion) CrearEvaluacionCompleta(
        Guid evaluadorId,
        decimal puntaje)
    {
        var (rubrica, criterio) = CrearRubricaEscalaCien();
        var asignacion = CrearAsignacionEnEstado(evaluadorId, EstadoAsignacionEvaluacion.EnProgreso);

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, puntaje, null)],
            null,
            _fechaBase.AddDays(3),
            evaluadorId);

        evaluacion.EnviarDefinitiva(rubrica, asignacion.FechaLimiteEvaluacion, _fechaBase.AddDays(4), evaluadorId);
        asignacion.CompletarEvaluacion(_fechaBase.AddDays(4), evaluadorId);

        return (asignacion, evaluacion);
    }

    private static AsignacionEvaluacion CrearAsignacionEnEstado(Guid evaluadorId, EstadoAsignacionEvaluacion estado)
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Consolidación",
            _jefaturaId,
            _fechaBase,
            _coordinadorId);

        var miembro = comite.AgregarMiembro(
            evaluadorId,
            TipoEvaluador.Externo,
            _fechaBase.AddDays(1),
            _coordinadorId);

        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaBase.AddDays(2),
            _fechaLimite,
            _coordinadorId);

        if (estado == EstadoAsignacionEvaluacion.EnProgreso)
        {
            asignacion.IniciarEvaluacion(_fechaBase.AddDays(3), evaluadorId);
        }

        return asignacion;
    }

    private static (RubricaEvaluacion Rubrica, CriterioEvaluacion Criterio) CrearRubricaEscalaCien()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Escala 100",
            _fechaBase,
            _coordinadorId);

        var criterio = rubrica.AgregarCriterio(
            "Global",
            "Puntaje global",
            100.00m,
            0.00m,
            100.00m,
            _fechaBase.AddMinutes(1),
            _coordinadorId);

        rubrica.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);
        return (rubrica, criterio);
    }

    private static void FijarEstadoRanking(ResultadoConsolidadoRanking resultado, EstadoRanking estado)
    {
        var propiedad = typeof(ResultadoConsolidadoRanking)
            .GetProperty(nameof(ResultadoConsolidadoRanking.EstadoConfirmacionRanking), BindingFlags.Instance | BindingFlags.Public);

        propiedad.Should().NotBeNull();
        propiedad!.SetValue(resultado, estado);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
