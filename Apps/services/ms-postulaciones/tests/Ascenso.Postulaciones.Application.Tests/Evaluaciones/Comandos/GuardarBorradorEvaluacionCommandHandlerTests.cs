using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comun;
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

public sealed class GuardarBorradorEvaluacionCommandHandlerTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968100-0000-7000-8000-000000000301");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968100-0000-7000-8000-000000000302");
    private static readonly Guid _jefaturaId = Guid.Parse("01968100-0000-7000-8000-000000000303");
    private static readonly Guid _coordinadorId = Guid.Parse("01968100-0000-7000-8000-000000000304");
    private static readonly Guid _evaluadorId = Guid.Parse("01968100-0000-7000-8000-000000000305");
    private static readonly Guid _otroEvaluadorId = Guid.Parse("01968100-0000-7000-8000-000000000306");
    private static readonly Guid _postulacionId = Guid.Parse("01968100-0000-7000-8000-000000000307");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora =
        new(2026, 11, 25, 15, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IConsultaAsignacionParaEvaluacionRepository _asignacionRepository =
        Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
    private readonly IComiteEvaluadorRepository _comiteRepository =
        Substitute.For<IComiteEvaluadorRepository>();
    private readonly IConsultaRubricaParaEvaluacionRepository _rubricaRepository =
        Substitute.For<IConsultaRubricaParaEvaluacionRepository>();
    private readonly IEvaluacionRepository _evaluacionRepository =
        Substitute.For<IEvaluacionRepository>();
    private readonly IBloqueadorRubricaEvaluacion _bloqueadorRubrica =
        Substitute.For<IBloqueadorRubricaEvaluacion>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GuardarBorradorEvaluacionCommandHandler _handler;

    public GuardarBorradorEvaluacionCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_evaluadorId);
        _unitOfWork.GuardarCambiosAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        _handler = new GuardarBorradorEvaluacionCommandHandler(
            _usuarioActual,
            _asignacionRepository,
            _comiteRepository,
            _rubricaRepository,
            _evaluacionRepository,
            _bloqueadorRubrica,
            _unitOfWork,
            new GuardarBorradorEvaluacionCommandValidator(),
            new RelojFijo(_ahora));
    }

    [Fact]
    public async Task Debe_CrearBorradorTransicionarAsignacionGuardarYDespuesBloquearRubricaPublicada_Cuando_EsPrimerBorrador()
    {
        using var cts = new CancellationTokenSource();
        var (comite, asignacion, rubrica, c1, _) = CrearEscenario();
        ConfigurarRepositorios(comite, asignacion, rubrica, null, cts.Token);

        var resultado = await _handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 16.50m, "Muy bien")],
                "Avance inicial"),
            cts.Token);

        resultado.EsCreacion.Should().BeTrue();
        resultado.Evaluacion.Estado.Should().Be("BORRADOR");
        resultado.Evaluacion.PuntajeTotalPonderado.Should().BeNull();
        resultado.Evaluacion.FechaEnvio.Should().BeNull();
        resultado.Evaluacion.Calificaciones.Should().ContainSingle();
        asignacion.Estado.Should().Be(EstadoAsignacionEvaluacion.EnProgreso);

        Received.InOrder(() =>
        {
            _evaluacionRepository.AgregarAsync(Arg.Any<Evaluacion>(), cts.Token);
            _unitOfWork.GuardarCambiosAsync(cts.Token);
            _bloqueadorRubrica.BloquearPorPrimeraCalificacionAsync(rubrica.Id, cts.Token);
        });
    }

    [Fact]
    public async Task Debe_NoVolverABloquearRubrica_Cuando_YaEstaBloqueada()
    {
        using var cts = new CancellationTokenSource();
        var (comite, asignacion, rubrica, c1, _) = CrearEscenario();
        rubrica.Bloquear(MotivoBloqueoRubrica.PrimeraCalificacion, _fechaBase.AddDays(4), UsuarioSistema.Id);
        ConfigurarRepositorios(comite, asignacion, rubrica, null, cts.Token);

        var resultado = await _handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 14.00m, null)]),
            cts.Token);

        resultado.EsCreacion.Should().BeTrue();
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
        await _bloqueadorRubrica.DidNotReceiveWithAnyArgs()
            .BloquearPorPrimeraCalificacionAsync(default, default);
    }

    [Fact]
    public async Task Debe_ActualizarBorradorExistenteConEsCreacionFalso_Cuando_YaExisteBorrador()
    {
        using var cts = new CancellationTokenSource();
        var (comite, asignacion, rubrica, c1, c2) = CrearEscenario();
        rubrica.Bloquear(MotivoBloqueoRubrica.PrimeraCalificacion, _fechaBase.AddDays(3), UsuarioSistema.Id);
        var evaluacionExistente = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 10.00m, "Inicial")],
            "Inicial",
            _fechaBase.AddDays(3),
            _evaluadorId);
        asignacion.IniciarEvaluacion(_fechaBase.AddDays(3), _evaluadorId);
        ConfigurarRepositorios(comite, asignacion, rubrica, evaluacionExistente, cts.Token);

        var resultado = await _handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [
                    new DatosCalificacionCriterioCommand(c1.Id, 18.00m, "Corregido"),
                    new DatosCalificacionCriterioCommand(c2.Id, 9.00m, "Agregado")
                ],
                "Actualizado"),
            cts.Token);

        resultado.EsCreacion.Should().BeFalse();
        resultado.Evaluacion.Version.Should().Be(2);
        resultado.Evaluacion.Calificaciones.Should().HaveCount(2);
        resultado.Evaluacion.ComentariosGenerales.Should().Be("Actualizado");
        await _evaluacionRepository.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarCon403ConfidencialSinGuardarNiBloquear_Cuando_AsignacionPerteneceAOtroEvaluador()
    {
        var (comite, asignacion, rubrica, c1, _) = CrearEscenario();
        _usuarioActual.UsuarioId.Returns(_otroEvaluadorId);
        ConfigurarRepositorios(comite, asignacion, rubrica, null, CancellationToken.None);

        var accion = () => _handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 10.00m, null)]),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluacionConfidencialException>()
            .Where(ex => ex.Codigo == "EVALUACION_CONFIDENCIAL" && ex.EstadoHttp == 403);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
        await _bloqueadorRubrica.DidNotReceiveWithAnyArgs()
            .BloquearPorPrimeraCalificacionAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_AsignacionComiteORubricaNoExisten()
    {
        var (comite, asignacion, _, c1, _) = CrearEscenario();

        // 1. Asignación no encontrada
        var accionSinAsignacion = () => _handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 10.00m, null)]),
            CancellationToken.None);
        await accionSinAsignacion.Should()
            .ThrowAsync<AsignacionEvaluacionNoEncontradaException>()
            .Where(ex => ex.Codigo == "ASIGNACION_NO_ENCONTRADA" && ex.EstadoHttp == 404);

        // 2. Comité no encontrado
        _asignacionRepository
            .ObtenerParaActualizarPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacion));
        var accionSinComite = () => _handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 10.00m, null)]),
            CancellationToken.None);
        await accionSinComite.Should()
            .ThrowAsync<ComiteNoEncontradoException>()
            .Where(ex => ex.Codigo == "COMITE_NO_ENCONTRADO" && ex.EstadoHttp == 404);

        // 3. Rúbrica no encontrada
        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));
        var accionSinRubrica = () => _handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 10.00m, null)]),
            CancellationToken.None);
        await accionSinRubrica.Should()
            .ThrowAsync<RubricaNoEncontradaException>()
            .Where(ex => ex.Codigo == "RUBRICA_NO_ENCONTRADA" && ex.EstadoHttp == 404);
    }

    [Fact]
    public async Task Debe_NoGuardarNiBloquearRubrica_Cuando_BorradorTienePuntajeFueraDeRango()
    {
        var (comite, asignacion, rubrica, c1, _) = CrearEscenario();
        ConfigurarRepositorios(comite, asignacion, rubrica, null, CancellationToken.None);

        var accion = () => _handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacion.Id,
                [new DatosCalificacionCriterioCommand(c1.Id, 25.00m, null)]),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PuntajeFueraDeRangoException>()
            .Where(ex => ex.Codigo == "PUNTAJE_FUERA_DE_RANGO" && ex.EstadoHttp == 422);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
        await _bloqueadorRubrica.DidNotReceiveWithAnyArgs()
            .BloquearPorPrimeraCalificacionAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                Guid.NewGuid(),
                [new DatosCalificacionCriterioCommand(Guid.NewGuid(), 10.00m, null)]),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(ex => ex.Codigo == "NO_AUTENTICADO" && ex.EstadoHttp == 401);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_PropagarCancelacion_Cuando_TokenEstaCancelado()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _asignacionRepository
            .ObtenerParaActualizarPorIdAsync(Arg.Any<Guid>(), Arg.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .Returns<Task<AsignacionEvaluacion?>>(_ => throw new OperationCanceledException(cts.Token));

        var accion = () => _handler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                Guid.NewGuid(),
                [new DatosCalificacionCriterioCommand(Guid.NewGuid(), 10.00m, null)]),
            cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    private void ConfigurarRepositorios(
        ComiteEvaluador comite,
        AsignacionEvaluacion asignacion,
        RubricaEvaluacion rubrica,
        Evaluacion? evaluacionExistente,
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
            .Returns(Task.FromResult(evaluacionExistente));
    }

    private static (
        ComiteEvaluador Comite,
        AsignacionEvaluacion Asignacion,
        RubricaEvaluacion Rubrica,
        CriterioEvaluacion C1,
        CriterioEvaluacion C2) CrearEscenario()
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
            _fechaBase.AddDays(20),
            _coordinadorId);

        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Excelencia",
            _fechaBase,
            _coordinadorId);
        var c1 = rubrica.AgregarCriterio("C1", "Desc 1", 60.00m, 0.00m, 20.00m, _fechaBase.AddMinutes(1), _coordinadorId);
        var c2 = rubrica.AgregarCriterio("C2", "Desc 2", 40.00m, 0.00m, 10.00m, _fechaBase.AddMinutes(2), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(3), _coordinadorId);

        return (comite, asignacion, rubrica, c1, c2);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
