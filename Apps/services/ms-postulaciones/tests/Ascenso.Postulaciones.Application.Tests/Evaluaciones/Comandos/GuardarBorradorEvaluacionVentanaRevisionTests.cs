using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.Comandos;

public sealed class GuardarBorradorEvaluacionVentanaRevisionTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968200-0000-7000-8000-000000000301");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968200-0000-7000-8000-000000000302");
    private static readonly Guid _jefaturaId = Guid.Parse("01968200-0000-7000-8000-000000000303");
    private static readonly Guid _coordinadorId = Guid.Parse("01968200-0000-7000-8000-000000000304");
    private static readonly Guid _evaluadorId = Guid.Parse("01968200-0000-7000-8000-000000000305");
    private static readonly Guid _otroEvaluadorId = Guid.Parse("01968200-0000-7000-8000-000000000306");
    private static readonly Guid _postulacionId = Guid.Parse("01968200-0000-7000-8000-000000000307");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _inicioVentana =
        new(2026, 11, 20, 4, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _cierreVentana =
        new(2026, 11, 30, 3, 59, 59, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IConsultaAsignacionParaEvaluacionRepository _asignacionRepository =
        Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient =
        Substitute.For<IConvocatoriaEvaluacionClient>();
    private readonly IComiteEvaluadorRepository _comiteRepository =
        Substitute.For<IComiteEvaluadorRepository>();
    private readonly IConsultaRubricaParaEvaluacionRepository _rubricaRepository =
        Substitute.For<IConsultaRubricaParaEvaluacionRepository>();
    private readonly IEvaluacionRepository _evaluacionRepository =
        Substitute.For<IEvaluacionRepository>();
    private readonly IBloqueadorRubricaEvaluacion _bloqueadorRubrica =
        Substitute.For<IBloqueadorRubricaEvaluacion>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public GuardarBorradorEvaluacionVentanaRevisionTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_evaluadorId);
        _unitOfWork.GuardarCambiosAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Publicada, true)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, false)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, true)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, false)]
    public async Task Debe_PermitirGuardarBorrador_Cuando_AhoraCoincideConLimitesExactosDeVentana(
        EstadoConvocatoriaExterna estadoConvocatoria,
        bool esLimiteInicio)
    {
        using var cts = new CancellationTokenSource();
        var ahora = esLimiteInicio ? _inicioVentana : _cierreVentana;
        var (comite, asignacion, rubrica, c1) = CrearEscenario();
        ConfigurarRepositorios(comite, asignacion, rubrica, cts.Token);
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, cts.Token)
            .Returns(Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(_convocatoriaId, estadoConvocatoria, _inicioVentana, _cierreVentana)));
        var handler = CrearHandler(ahora);

        var resultado = await handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 16.00m, "En límite")]),
            cts.Token);

        resultado.EsCreacion.Should().BeTrue();
        resultado.Evaluacion.Estado.Should().Be("BORRADOR");
        await _convocatoriaClient.Received(1)
            .ObtenerVentanaRevisionAsync(_convocatoriaId, cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Publicada)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada)]
    public async Task Debe_RechazarCon409PeriodoRevisionCerradoSinConsultarComiteNiPersistir_Cuando_VentanaYaCerro(
        EstadoConvocatoriaExterna estadoConvocatoria)
    {
        var (comite, asignacion, rubrica, c1) = CrearEscenario();
        ConfigurarRepositorios(comite, asignacion, rubrica, CancellationToken.None);
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(_convocatoriaId, estadoConvocatoria, _inicioVentana, _cierreVentana)));
        var handler = CrearHandler(_cierreVentana.AddTicks(1));

        var accion = () => handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 16.00m, null)]),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PeriodoRevisionCerradoException>()
            .Where(ex => ex.Codigo == "PERIODO_REVISION_CERRADO" && ex.EstadoHttp == 409);

        await VerificarSinEfectosSecundariosAsync();
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Borrador, 0)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, -1)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, -1)]
    public async Task Debe_RechazarCon409PeriodoRevisionNoIniciadoSinPersistir_Cuando_VentanaNoHaIniciado(
        EstadoConvocatoriaExterna estadoConvocatoria,
        int ticksDesdeInicio)
    {
        var (comite, asignacion, rubrica, c1) = CrearEscenario();
        ConfigurarRepositorios(comite, asignacion, rubrica, CancellationToken.None);
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(_convocatoriaId, estadoConvocatoria, _inicioVentana, _cierreVentana)));
        var handler = CrearHandler(_inicioVentana.AddTicks(ticksDesdeInicio));

        var accion = () => handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 16.00m, null)]),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PeriodoRevisionNoIniciadoException>()
            .Where(ex => ex.Codigo == "PERIODO_REVISION_NO_INICIADO" && ex.EstadoHttp == 409);

        await VerificarSinEfectosSecundariosAsync();
    }

    [Fact]
    public async Task Debe_RechazarCon409ConvocatoriaCanceladaSinPersistir_Cuando_ConvocatoriaEstaCancelada()
    {
        var (comite, asignacion, rubrica, c1) = CrearEscenario();
        ConfigurarRepositorios(comite, asignacion, rubrica, CancellationToken.None);
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Cancelada, _inicioVentana, _cierreVentana)));
        var handler = CrearHandler(_inicioVentana.AddDays(2));

        var accion = () => handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 16.00m, null)]),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ConvocatoriaCanceladaException>()
            .Where(ex => ex.Codigo == "CONVOCATORIA_CANCELADA" && ex.EstadoHttp == 409);

        await VerificarSinEfectosSecundariosAsync();
    }

    [Fact]
    public async Task Debe_RechazarCon404ConvocatoriaNoEncontradaSinPersistir_Cuando_ClienteRetornaNulo()
    {
        var (comite, asignacion, rubrica, c1) = CrearEscenario();
        ConfigurarRepositorios(comite, asignacion, rubrica, CancellationToken.None);
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VentanaRevisionConvocatoria?>(null));
        var handler = CrearHandler(_inicioVentana.AddDays(2));

        var accion = () => handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 16.00m, null)]),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ConvocatoriaNoEncontradaException>()
            .Where(ex => ex.Codigo == "CONVOCATORIA_NO_ENCONTRADA" && ex.EstadoHttp == 404);

        await VerificarSinEfectosSecundariosAsync();
    }

    [Fact]
    public async Task Debe_FallarCerradoCon503DependenciaNoDisponibleSinPersistir_Cuando_MsPrgNoResponde()
    {
        var (comite, asignacion, rubrica, c1) = CrearEscenario();
        ConfigurarRepositorios(comite, asignacion, rubrica, CancellationToken.None);
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns<Task<VentanaRevisionConvocatoria?>>(_ => throw new DependenciaNoDisponibleException());
        var handler = CrearHandler(_inicioVentana.AddDays(2));

        var accion = () => handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 16.00m, null)]),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<DependenciaNoDisponibleException>()
            .Where(ex => ex.Codigo == "DEPENDENCIA_NO_DISPONIBLE" && ex.EstadoHttp == 503);

        await VerificarSinEfectosSecundariosAsync();
    }

    [Fact]
    public async Task Debe_NoInvocarMsPrg_Cuando_AsignacionPerteneceAOtroEvaluador()
    {
        var (comite, asignacion, rubrica, c1) = CrearEscenario();
        _usuarioActual.UsuarioId.Returns(_otroEvaluadorId);
        ConfigurarRepositorios(comite, asignacion, rubrica, CancellationToken.None);
        var handler = CrearHandler(_inicioVentana.AddDays(2));

        var accion = () => handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 16.00m, null)]),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluacionConfidencialException>()
            .Where(ex => ex.Codigo == "EVALUACION_CONFIDENCIAL" && ex.EstadoHttp == 403);

        await _convocatoriaClient.DidNotReceiveWithAnyArgs()
            .ObtenerVentanaRevisionAsync(default, default);
        await VerificarSinEfectosSecundariosAsync();
    }

    private GuardarBorradorEvaluacionCommandHandler CrearHandler(DateTimeOffset ahora)
        => new(
            _usuarioActual,
            _asignacionRepository,
            _convocatoriaClient,
            _comiteRepository,
            _rubricaRepository,
            _evaluacionRepository,
            _bloqueadorRubrica,
            _unitOfWork,
            new GuardarBorradorEvaluacionCommandValidator(),
            new RelojFijo(ahora));

    private async Task VerificarSinEfectosSecundariosAsync()
    {
        await _comiteRepository.DidNotReceiveWithAnyArgs().ObtenerPorIdAsync(default, default);
        await _rubricaRepository.DidNotReceiveWithAnyArgs()
            .ObtenerPorProgramaYConvocatoriaAsync(default, default, default);
        await _evaluacionRepository.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
        await _bloqueadorRubrica.DidNotReceiveWithAnyArgs()
            .BloquearPorPrimeraCalificacionAsync(default, default);
    }

    private void ConfigurarRepositorios(
        ComiteEvaluador comite,
        AsignacionEvaluacion asignacion,
        RubricaEvaluacion rubrica,
        CancellationToken ct)
    {
        _asignacionRepository
            .ObtenerParaActualizarPorIdAsync(asignacion.Id, ct)
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacion));
        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, ct)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));
        _rubricaRepository
            .ObtenerPorProgramaYConvocatoriaAsync(comite.ProgramaBecaId, comite.ConvocatoriaId, ct)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));
        _evaluacionRepository
            .ObtenerParaActualizarPorAsignacionAsync(asignacion.Id, ct)
            .Returns(Task.FromResult<Evaluacion?>(null));
    }

    private static (
        ComiteEvaluador Comite,
        AsignacionEvaluacion Asignacion,
        RubricaEvaluacion Rubrica,
        CriterioEvaluacion C1) CrearEscenario()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia",
            _jefaturaId,
            _fechaBase,
            _coordinadorId);
        var miembro = comite.AgregarMiembro(
            _evaluadorId,
            TipoEvaluador.Externo,
            _fechaBase.AddDays(1),
            _coordinadorId);
        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaBase.AddDays(2),
            _cierreVentana.AddDays(10),
            _coordinadorId);

        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Excelencia",
            _fechaBase,
            _coordinadorId);
        var c1 = rubrica.AgregarCriterio("C1", "Desc 1", 100.00m, 0.00m, 20.00m, _fechaBase.AddMinutes(1), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);

        return (comite, asignacion, rubrica, c1);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
