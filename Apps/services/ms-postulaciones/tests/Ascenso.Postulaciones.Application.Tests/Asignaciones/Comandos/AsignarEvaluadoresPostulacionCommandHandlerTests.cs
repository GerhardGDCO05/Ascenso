using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AsignarEvaluadoresPostulacion;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Comandos;

public sealed class AsignarEvaluadoresPostulacionCommandHandlerTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000401");
    private static readonly Guid _programaBecaId = Guid.Parse("01967000-0000-7000-8000-000000000402");
    private static readonly Guid _convocatoriaId = Guid.Parse("01967000-0000-7000-8000-000000000403");
    private static readonly Guid _jefaturaId = Guid.Parse("01967000-0000-7000-8000-000000000404");
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000405");
    private static readonly DateTimeOffset _fechaCreacionComite =
        new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora =
        new(2026, 11, 24, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite =
        new(2026, 12, 10, 23, 59, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IComiteEvaluadorRepository _comiteRepository = Substitute.For<IComiteEvaluadorRepository>();
    private readonly IAsignacionEvaluacionRepository _asignacionRepository = Substitute.For<IAsignacionEvaluacionRepository>();
    private readonly IExcepcionEvaluadorUnicoRepository _excepcionRepository = Substitute.For<IExcepcionEvaluadorUnicoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AsignarEvaluadoresPostulacionCommandHandler _handler;

    public AsignarEvaluadoresPostulacionCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _asignacionRepository
            .ListarVigentesPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AsignacionEvaluacion>>([]));
        _asignacionRepository
            .ObtenerCargaPorMiembroAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>()));
        _asignacionRepository
            .AgregarRangoAsync(Arg.Any<IReadOnlyCollection<AsignacionEvaluacion>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _excepcionRepository
            .ObtenerPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(null));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(2));

        _handler = new AsignarEvaluadoresPostulacionCommandHandler(
            _usuarioActual,
            _comiteRepository,
            _asignacionRepository,
            _excepcionRepository,
            _unitOfWork,
            new AsignarEvaluadoresPostulacionCommandValidator(),
            new RelojFijo(_ahora));
    }

    [Fact]
    public async Task Debe_AsignarManualmenteYGuardarUnaSolaVez_Cuando_LosDatosSonValidos()
    {
        using var cts = new CancellationTokenSource();
        var (comite, evaluador1, evaluador2) = CrearComiteConEvaluadores();
        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var comando = new AsignarEvaluadoresPostulacionCommand(
            comite.Id,
            _postulacionId,
            _fechaLimite,
            UsuariosEvaluadoresIds: [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId]);

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.PostulacionId.Should().Be(_postulacionId);
        resultado.Asignaciones.Should().HaveCount(2);
        resultado.Asignaciones.Should().AllSatisfy(item =>
        {
            item.ComiteEvaluadorId.Should().Be(comite.Id);
            item.ConvocatoriaId.Should().Be(_convocatoriaId);
            item.PostulacionId.Should().Be(_postulacionId);
            item.Estado.Should().Be("PENDIENTE");
            item.FechaAsignacion.Should().Be(_ahora);
            item.FechaLimiteEvaluacion.Should().Be(_fechaLimite);
            item.Version.Should().Be(1);
        });

        await _asignacionRepository.DidNotReceiveWithAnyArgs()
            .ObtenerCargaPorMiembroAsync(default, default);
        await _asignacionRepository.Received(1)
            .AgregarRangoAsync(
                Arg.Is<IReadOnlyCollection<AsignacionEvaluacion>>(lista => lista.Count == 2),
                cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_AsignarAutomaticamentePorMenorCargaYGuardarUnaSolaVez_Cuando_SeIndicaCantidad()
    {
        using var cts = new CancellationTokenSource();
        var (comite, evaluador1, evaluador2) = CrearComiteConEvaluadores();
        var jefatura = comite.Miembros.Single(m => m.Rol == RolMiembroComite.Jefatura);

        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        IReadOnlyDictionary<Guid, int> carga = new Dictionary<Guid, int>
        {
            [jefatura.Id] = 5,
            [evaluador1.Id] = 2,
            [evaluador2.Id] = 0
        };

        _asignacionRepository
            .ObtenerCargaPorMiembroAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult(carga));

        var comando = new AsignarEvaluadoresPostulacionCommand(
            comite.Id,
            _postulacionId,
            _fechaLimite,
            CantidadEvaluadores: 2);

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.PostulacionId.Should().Be(_postulacionId);
        resultado.Asignaciones.Should().HaveCount(2);
        resultado.Asignaciones[0].MiembroComiteId.Should().Be(evaluador2.Id);
        resultado.Asignaciones[1].MiembroComiteId.Should().Be(evaluador1.Id);
        resultado.Asignaciones[0].FechaAsignacion.Should().Be(_ahora);

        await _asignacionRepository.Received(1)
            .ObtenerCargaPorMiembroAsync(comite.Id, cts.Token);
        await _asignacionRepository.Received(1)
            .AgregarRangoAsync(
                Arg.Is<IReadOnlyCollection<AsignacionEvaluacion>>(lista => lista.Count == 2),
                cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarCon404SinGuardar_Cuando_ElComiteNoExiste()
    {
        var comiteId = Guid.NewGuid();
        _comiteRepository
            .ObtenerPorIdAsync(comiteId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(null));

        var comando = new AsignarEvaluadoresPostulacionCommand(
            comiteId,
            _postulacionId,
            _fechaLimite,
            CantidadEvaluadores: 2);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ComiteNoEncontradoException>()
            .Where(ex => ex.Codigo == "COMITE_NO_ENCONTRADO" && ex.EstadoHttp == 404);

        await _asignacionRepository.DidNotReceiveWithAnyArgs()
            .ListarVigentesPorPostulacionAsync(default, default);
        await _asignacionRepository.DidNotReceiveWithAnyArgs()
            .AgregarRangoAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_NoGuardarNada_Cuando_ElDominioRechazaLaAsignacion()
    {
        var (comite, evaluador1, _) = CrearComiteConEvaluadores();
        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var comando = new AsignarEvaluadoresPostulacionCommand(
            comite.Id,
            _postulacionId,
            _fechaLimite,
            UsuariosEvaluadoresIds: [evaluador1.UsuarioEvaluadorId]);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluadoresInsuficientesException>()
            .Where(ex => ex.Codigo == "EVALUACIONES_INSUFICIENTES" && ex.EstadoHttp == 422);

        await _asignacionRepository.DidNotReceiveWithAnyArgs()
            .AgregarRangoAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_PropagarAsignacionDuplicada_Cuando_OcurreCarreraAlGuardarCambios()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConEvaluadores();
        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new AsignacionDuplicadaException()));

        var comando = new AsignarEvaluadoresPostulacionCommand(
            comite.Id,
            _postulacionId,
            _fechaLimite,
            UsuariosEvaluadoresIds: [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId]);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<AsignacionDuplicadaException>()
            .Where(ex => ex.Codigo == "ASIGNACION_DUPLICADA" && ex.EstadoHttp == 409);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_ElComandoEsInvalido()
    {
        var comando = new AsignarEvaluadoresPostulacionCommand(
            Guid.Empty,
            _postulacionId,
            _fechaLimite,
            CantidadEvaluadores: 2);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();

        await _comiteRepository.DidNotReceiveWithAnyArgs().ObtenerPorIdAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var comando = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            _postulacionId,
            _fechaLimite,
            CantidadEvaluadores: 2);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(ex => ex.Codigo == "NO_AUTENTICADO" && ex.EstadoHttp == 401);

        await _comiteRepository.DidNotReceiveWithAnyArgs().ObtenerPorIdAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_PropagarCancelacion_Cuando_ElTokenEstaCancelado()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var comiteId = Guid.NewGuid();
        _comiteRepository
            .ObtenerPorIdAsync(comiteId, cts.Token)
            .Returns(Task.FromCanceled<ComiteEvaluador?>(cts.Token));

        var comando = new AsignarEvaluadoresPostulacionCommand(
            comiteId,
            _postulacionId,
            _fechaLimite,
            CantidadEvaluadores: 2);

        var accion = () => _handler.HandleAsync(comando, cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static (ComiteEvaluador Comite, MiembroComite Evaluador1, MiembroComite Evaluador2)
        CrearComiteConEvaluadores()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia 2026-II",
            _jefaturaId,
            _fechaCreacionComite,
            _coordinadorId);

        var evaluador1 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacionComite.AddDays(1),
            _coordinadorId);

        var evaluador2 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaCreacionComite.AddDays(2),
            _coordinadorId);

        return (comite, evaluador1, evaluador2);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
