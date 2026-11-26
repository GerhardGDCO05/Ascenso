using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.EnviarEvaluacionDefinitiva;
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
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.Comandos;

public sealed class EnviarEvaluacionDefinitivaCommandHandlerTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968100-0000-7000-8000-000000000401");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968100-0000-7000-8000-000000000402");
    private static readonly Guid _jefaturaId = Guid.Parse("01968100-0000-7000-8000-000000000403");
    private static readonly Guid _coordinadorId = Guid.Parse("01968100-0000-7000-8000-000000000404");
    private static readonly Guid _evaluadorId = Guid.Parse("01968100-0000-7000-8000-000000000405");
    private static readonly Guid _otroEvaluadorId = Guid.Parse("01968100-0000-7000-8000-000000000406");
    private static readonly Guid _postulacionId = Guid.Parse("01968100-0000-7000-8000-000000000407");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite =
        new(2026, 12, 10, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora =
        new(2026, 11, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IConsultaAsignacionParaEvaluacionRepository _asignacionRepository =
        Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient =
        Substitute.For<IConvocatoriaEvaluacionClient>();
    private readonly IEvaluacionRepository _evaluacionRepository =
        Substitute.For<IEvaluacionRepository>();
    private readonly IComiteEvaluadorRepository _comiteRepository =
        Substitute.For<IComiteEvaluadorRepository>();
    private readonly IConsultaRubricaParaEvaluacionRepository _rubricaRepository =
        Substitute.For<IConsultaRubricaParaEvaluacionRepository>();
    private readonly IBloqueadorRubricaEvaluacion _bloqueadorRubrica =
        Substitute.For<IBloqueadorRubricaEvaluacion>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public EnviarEvaluacionDefinitivaCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_evaluadorId);
        _unitOfWork.GuardarCambiosAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(
                    call.ArgAt<Guid>(0),
                    EstadoConvocatoriaExterna.Publicada,
                    _ahora.AddDays(-10),
                    _fechaLimite.AddDays(10))));
    }

    [Fact]
    public async Task Debe_EnviarDefinitivaCompletarAsignacionYGuardarUnaSolaVez_Cuando_BorradorEstaCompleto()
    {
        using var cts = new CancellationTokenSource();
        var (comite, asignacion, rubrica, evaluacion) = CrearEscenarioConBorrador(completo: true, bloquearRubrica: true);
        ConfigurarRepositorios(comite, asignacion, rubrica, evaluacion, cts.Token);
        var handler = CrearHandler(_ahora);

        var resultado = await handler.HandleAsync(
            new EnviarEvaluacionDefinitivaCommand(asignacion.Id),
            cts.Token);

        resultado.Estado.Should().Be("ENVIADA_DEFINITIVA");
        resultado.PuntajeTotalPonderado.Should().Be(77.00m);
        resultado.FechaEnvio.Should().Be(_ahora);
        asignacion.Estado.Should().Be(EstadoAsignacionEvaluacion.Completada);

        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
        await _bloqueadorRubrica.DidNotReceiveWithAnyArgs()
            .BloquearPorPrimeraCalificacionAsync(default, default);
    }

    [Fact]
    public async Task Debe_EjecutarResguardoIdempotenteDeBloqueo_Cuando_RubricaSiguePublicadaAlEnviar()
    {
        using var cts = new CancellationTokenSource();
        var (comite, asignacion, rubrica, evaluacion) = CrearEscenarioConBorrador(completo: true, bloquearRubrica: false);
        ConfigurarRepositorios(comite, asignacion, rubrica, evaluacion, cts.Token);
        var handler = CrearHandler(_ahora);

        var resultado = await handler.HandleAsync(
            new EnviarEvaluacionDefinitivaCommand(asignacion.Id),
            cts.Token);

        resultado.Estado.Should().Be("ENVIADA_DEFINITIVA");
        Received.InOrder(() =>
        {
            _unitOfWork.GuardarCambiosAsync(cts.Token);
            _bloqueadorRubrica.BloquearPorPrimeraCalificacionAsync(rubrica.Id, cts.Token);
        });
    }

    [Fact]
    public async Task Debe_RechazarCon404SinGuardar_Cuando_NoExisteBorradorPrevio()
    {
        var (comite, asignacion, rubrica, _) = CrearEscenarioConBorrador(completo: true, bloquearRubrica: true);
        ConfigurarRepositorios(comite, asignacion, rubrica, null, CancellationToken.None);
        var handler = CrearHandler(_ahora);

        var accion = () => handler.HandleAsync(
            new EnviarEvaluacionDefinitivaCommand(asignacion.Id),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluacionNoEncontradaException>()
            .Where(ex => ex.Codigo == "EVALUACION_NO_ENCONTRADA" && ex.EstadoHttp == 404);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422IncompletaSinGuardar_Cuando_FaltanCriterios()
    {
        var (comite, asignacion, rubrica, evaluacion) = CrearEscenarioConBorrador(completo: false, bloquearRubrica: true);
        ConfigurarRepositorios(comite, asignacion, rubrica, evaluacion, CancellationToken.None);
        var handler = CrearHandler(_ahora);

        var accion = () => handler.HandleAsync(
            new EnviarEvaluacionDefinitivaCommand(asignacion.Id),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluacionIncompletaException>()
            .Where(ex => ex.Codigo == "EVALUACION_INCOMPLETA" && ex.EstadoHttp == 422);
        asignacion.Estado.Should().Be(EstadoAsignacionEvaluacion.EnProgreso);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409YaEnviadaSinGuardar_Cuando_EvaluacionYaFueEnviada()
    {
        var (comite, asignacion, rubrica, evaluacion) = CrearEscenarioConBorrador(completo: true, bloquearRubrica: true);
        evaluacion.EnviarDefinitiva(rubrica, asignacion.FechaLimiteEvaluacion, _ahora.AddHours(-1), _evaluadorId);
        asignacion.CompletarEvaluacion(_ahora.AddHours(-1), _evaluadorId);
        ConfigurarRepositorios(comite, asignacion, rubrica, evaluacion, CancellationToken.None);
        var handler = CrearHandler(_ahora);

        var accion = () => handler.HandleAsync(
            new EnviarEvaluacionDefinitivaCommand(asignacion.Id),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluacionYaEnviadaException>()
            .Where(ex => ex.Codigo == "EVALUACION_YA_ENVIADA" && ex.EstadoHttp == 409);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409PlazoVencidoSinGuardar_Cuando_PlazoYaVencio()
    {
        var (comite, asignacion, rubrica, evaluacion) = CrearEscenarioConBorrador(completo: true, bloquearRubrica: true);
        ConfigurarRepositorios(comite, asignacion, rubrica, evaluacion, CancellationToken.None);
        var handler = CrearHandler(_fechaLimite.AddSeconds(1));

        var accion = () => handler.HandleAsync(
            new EnviarEvaluacionDefinitivaCommand(asignacion.Id),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PlazoEvaluacionVencidoException>()
            .Where(ex => ex.Codigo == "PLAZO_EVALUACION_VENCIDO" && ex.EstadoHttp == 409);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon403ConfidencialSinGuardar_Cuando_AsignacionPerteneceAOtroEvaluador()
    {
        var (comite, asignacion, rubrica, evaluacion) = CrearEscenarioConBorrador(completo: true, bloquearRubrica: true);
        _usuarioActual.UsuarioId.Returns(_otroEvaluadorId);
        ConfigurarRepositorios(comite, asignacion, rubrica, evaluacion, CancellationToken.None);
        var handler = CrearHandler(_ahora);

        var accion = () => handler.HandleAsync(
            new EnviarEvaluacionDefinitivaCommand(asignacion.Id),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluacionConfidencialException>()
            .Where(ex => ex.Codigo == "EVALUACION_CONFIDENCIAL" && ex.EstadoHttp == 403);
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
        var handler = CrearHandler(_ahora);

        var accion = () => handler.HandleAsync(
            new EnviarEvaluacionDefinitivaCommand(Guid.NewGuid()),
            cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    private EnviarEvaluacionDefinitivaCommandHandler CrearHandler(DateTimeOffset ahora)
        => new(
            _usuarioActual,
            _asignacionRepository,
            _convocatoriaClient,
            _evaluacionRepository,
            _comiteRepository,
            _rubricaRepository,
            _bloqueadorRubrica,
            _unitOfWork,
            new EnviarEvaluacionDefinitivaCommandValidator(),
            new RelojFijo(ahora));

    private void ConfigurarRepositorios(
        ComiteEvaluador comite,
        AsignacionEvaluacion asignacion,
        RubricaEvaluacion rubrica,
        Evaluacion? evaluacion,
        CancellationToken ct)
    {
        _asignacionRepository
            .ObtenerParaActualizarPorIdAsync(asignacion.Id, ct)
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacion));
        _evaluacionRepository
            .ObtenerParaActualizarPorAsignacionAsync(asignacion.Id, ct)
            .Returns(Task.FromResult(evaluacion));
        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, ct)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));
        _rubricaRepository
            .ObtenerPorProgramaYConvocatoriaAsync(comite.ProgramaBecaId, comite.ConvocatoriaId, ct)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));
    }

    private static (
        ComiteEvaluador Comite,
        AsignacionEvaluacion Asignacion,
        RubricaEvaluacion Rubrica,
        Evaluacion Evaluacion) CrearEscenarioConBorrador(bool completo, bool bloquearRubrica)
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
            _fechaLimite,
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

        DatosCalificacionCriterio[] calificaciones = completo
            ? [
                new DatosCalificacionCriterio(c1.Id, 15.00m, "C1"),
                new DatosCalificacionCriterio(c2.Id, 8.00m, "C2")
            ]
            : [new DatosCalificacionCriterio(c1.Id, 15.00m, "C1")];

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            calificaciones,
            "Borrador listo",
            _fechaBase.AddDays(5),
            _evaluadorId);
        asignacion.IniciarEvaluacion(_fechaBase.AddDays(5), _evaluadorId);

        if (bloquearRubrica)
        {
            rubrica.Bloquear(MotivoBloqueoRubrica.PrimeraCalificacion, _fechaBase.AddDays(5), UsuarioSistema.Id);
        }

        return (comite, asignacion, rubrica, evaluacion);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
