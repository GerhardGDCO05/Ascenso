using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AutorizarExcepcionEvaluadorUnico;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Comandos;

public sealed class AutorizarExcepcionEvaluadorUnicoCommandHandlerTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000701");
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000702");
    private static readonly DateTimeOffset _ahora =
        new(2026, 11, 24, 16, 0, 0, TimeSpan.Zero);
    private const string JustificacionValida = "Único evaluador disponible en el comité para esta especialidad.";

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IExcepcionEvaluadorUnicoRepository _excepcionRepository =
        Substitute.For<IExcepcionEvaluadorUnicoRepository>();
    private readonly IAsignacionEvaluacionRepository _asignacionRepository =
        Substitute.For<IAsignacionEvaluacionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AutorizarExcepcionEvaluadorUnicoCommandHandler _handler;

    public AutorizarExcepcionEvaluadorUnicoCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _excepcionRepository
            .ObtenerPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(null));
        _excepcionRepository
            .AgregarAsync(Arg.Any<AutorizacionExcepcionEvaluadorUnico>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _asignacionRepository
            .ListarVigentesPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AsignacionEvaluacion>>([]));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new AutorizarExcepcionEvaluadorUnicoCommandHandler(
            _usuarioActual,
            _excepcionRepository,
            _asignacionRepository,
            _unitOfWork,
            new AutorizarExcepcionEvaluadorUnicoCommandValidator(),
            new RelojFijo(_ahora));
    }

    [Fact]
    public async Task Debe_AutorizarYGuardarUnaSolaVezConAutorFechaYCorrelacion_Cuando_LosDatosSonValidos()
    {
        using var cts = new CancellationTokenSource();
        AutorizacionExcepcionEvaluadorUnico? persistida = null;
        _excepcionRepository
            .AgregarAsync(
                Arg.Do<AutorizacionExcepcionEvaluadorUnico>(entidad => persistida = entidad),
                cts.Token)
            .Returns(Task.CompletedTask);

        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(
            _postulacionId,
            $"   {JustificacionValida}   ");

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        persistida.Should().NotBeNull();
        persistida!.CorrelacionId.Should().NotBe(Guid.Empty);
        persistida.UsuarioAutorId.Should().Be(_coordinadorId);
        persistida.FechaCreacion.Should().Be(_ahora);

        resultado.Id.Should().Be(persistida.Id);
        resultado.PostulacionId.Should().Be(_postulacionId);
        resultado.Justificacion.Should().Be(JustificacionValida);
        resultado.UsuarioAutorizadorId.Should().Be(_coordinadorId);
        resultado.FechaAutorizacion.Should().Be(_ahora);

        await _excepcionRepository.Received(1)
            .ObtenerPorPostulacionAsync(_postulacionId, cts.Token);
        await _asignacionRepository.Received(1)
            .ListarVigentesPorPostulacionAsync(_postulacionId, cts.Token);
        await _excepcionRepository.Received(1)
            .AgregarAsync(persistida, cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarCon409SinGuardar_Cuando_YaExisteUnaExcepcionAutorizada()
    {
        var existente = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            JustificacionValida,
            _ahora.AddHours(-1),
            _coordinadorId,
            Guid.NewGuid());

        _excepcionRepository
            .ObtenerPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(existente));

        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(_postulacionId, JustificacionValida);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ExcepcionEvaluadorUnicoYaAutorizadaException>()
            .Where(ex => ex.Codigo == "EXCEPCION_YA_AUTORIZADA" && ex.EstadoHttp == 409);

        await _excepcionRepository.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422SinGuardar_Cuando_PostulacionTieneDosAsignacionesVigentes()
    {
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Excelencia",
            Guid.NewGuid(),
            _ahora.AddDays(-5),
            _coordinadorId);
        var miembro1 = comite.AgregarMiembro(Guid.NewGuid(), TipoEvaluador.Interno, _ahora.AddDays(-4), _coordinadorId);
        var miembro2 = comite.AgregarMiembro(Guid.NewGuid(), TipoEvaluador.Externo, _ahora.AddDays(-3), _coordinadorId);

        var vigentes = new[]
        {
            AsignacionEvaluacion.Crear(comite, miembro1.Id, _postulacionId, _ahora.AddDays(-1), _ahora.AddDays(10), _coordinadorId),
            AsignacionEvaluacion.Crear(comite, miembro2.Id, _postulacionId, _ahora.AddDays(-1), _ahora.AddDays(10), _coordinadorId)
        };

        _asignacionRepository
            .ListarVigentesPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AsignacionEvaluacion>>(vigentes));

        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(_postulacionId, JustificacionValida);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ExcepcionEvaluadorUnicoNoAplicableException>()
            .Where(ex => ex.Codigo == "EXCEPCION_NO_APLICABLE" && ex.EstadoHttp == 422);

        await _excepcionRepository.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_ElComandoEsInvalido()
    {
        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(Guid.Empty, "corta");

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();

        await _excepcionRepository.DidNotReceiveWithAnyArgs().ObtenerPorPostulacionAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(_postulacionId, JustificacionValida);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(ex => ex.Codigo == "NO_AUTENTICADO" && ex.EstadoHttp == 401);

        await _excepcionRepository.DidNotReceiveWithAnyArgs().ObtenerPorPostulacionAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_PropagarDuplicadoYConflictoConcurrencia_Cuando_OcurreCarreraAlGuardarCambios()
    {
        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(_postulacionId, JustificacionValida);

        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ExcepcionEvaluadorUnicoYaAutorizadaException()));

        var accionDuplicado = () => _handler.HandleAsync(comando, CancellationToken.None);
        await accionDuplicado.Should()
            .ThrowAsync<ExcepcionEvaluadorUnicoYaAutorizadaException>()
            .Where(ex => ex.Codigo == "EXCEPCION_YA_AUTORIZADA" && ex.EstadoHttp == 409);

        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ExcepcionConflictoConcurrencia()));

        var accionConcurrencia = () => _handler.HandleAsync(comando, CancellationToken.None);
        await accionConcurrencia.Should()
            .ThrowAsync<ExcepcionConflictoConcurrencia>()
            .Where(ex => ex.Codigo == "CONFLICTO_CONCURRENCIA" && ex.EstadoHttp == 409);
    }

    [Fact]
    public async Task Debe_PropagarCancelacion_Cuando_ElTokenEstaCancelado()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _excepcionRepository
            .ObtenerPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromCanceled<AutorizacionExcepcionEvaluadorUnico?>(cts.Token));

        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(_postulacionId, JustificacionValida);

        var accion = () => _handler.HandleAsync(comando, cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
