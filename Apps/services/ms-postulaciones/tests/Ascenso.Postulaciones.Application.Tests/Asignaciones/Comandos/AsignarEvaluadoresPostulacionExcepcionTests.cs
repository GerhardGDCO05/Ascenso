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
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Comandos;

public sealed class AsignarEvaluadoresPostulacionExcepcionTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000901");
    private static readonly Guid _programaBecaId = Guid.Parse("01967000-0000-7000-8000-000000000902");
    private static readonly Guid _convocatoriaId = Guid.Parse("01967000-0000-7000-8000-000000000903");
    private static readonly Guid _jefaturaId = Guid.Parse("01967000-0000-7000-8000-000000000904");
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000905");
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
    private readonly AsignarEvaluadoresPostulacionCommandValidator _validator = new();
    private readonly AsignarEvaluadoresPostulacionCommandHandler _handler;

    public AsignarEvaluadoresPostulacionExcepcionTests()
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
            .Returns(Task.FromResult(1));

        _handler = new AsignarEvaluadoresPostulacionCommandHandler(
            _usuarioActual,
            _comiteRepository,
            _asignacionRepository,
            _excepcionRepository,
            _unitOfWork,
            _validator,
            new RelojFijo(_ahora));
    }

    [Fact]
    public void Debe_AceptarCantidadUnoYRechazarCantidadCero_Cuando_SeValidaElComando()
    {
        var comandoUno = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fechaLimite,
            UsuariosEvaluadoresIds: null,
            CantidadEvaluadores: 1);

        var comandoCero = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fechaLimite,
            UsuariosEvaluadoresIds: null,
            CantidadEvaluadores: 0);

        _validator.Validate(comandoUno).IsValid.Should().BeTrue();
        _validator.Validate(comandoCero).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_AsignarUnEvaluadorManualmente_Cuando_ExisteExcepcionAutorizada()
    {
        using var cts = new CancellationTokenSource();
        var (comite, evaluador1, _) = CrearComiteConEvaluadores();
        var excepcion = CrearExcepcionAutorizada();

        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));
        _excepcionRepository
            .ObtenerPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(excepcion));

        var comando = new AsignarEvaluadoresPostulacionCommand(
            comite.Id,
            _postulacionId,
            _fechaLimite,
            UsuariosEvaluadoresIds: [evaluador1.UsuarioEvaluadorId]);

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.PostulacionId.Should().Be(_postulacionId);
        resultado.Asignaciones.Should().ContainSingle();
        resultado.Asignaciones[0].MiembroComiteId.Should().Be(evaluador1.Id);
        resultado.Asignaciones[0].UsuarioEvaluadorId.Should().Be(evaluador1.UsuarioEvaluadorId);
        resultado.Asignaciones[0].Estado.Should().Be("PENDIENTE");

        await _excepcionRepository.Received(1)
            .ObtenerPorPostulacionAsync(_postulacionId, cts.Token);
        await _asignacionRepository.Received(1)
            .AgregarRangoAsync(
                Arg.Is<IReadOnlyCollection<AsignacionEvaluacion>>(lista => lista.Count == 1),
                cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_AsignarUnEvaluadorAutomaticamentePorMenorCarga_Cuando_ExisteExcepcionAutorizada()
    {
        using var cts = new CancellationTokenSource();
        var (comite, evaluador1, evaluador2) = CrearComiteConEvaluadores();
        var jefatura = comite.Miembros.Single(m => m.Rol == RolMiembroComite.Jefatura);
        var excepcion = CrearExcepcionAutorizada();

        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));
        _excepcionRepository
            .ObtenerPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(excepcion));

        IReadOnlyDictionary<Guid, int> carga = new Dictionary<Guid, int>
        {
            [jefatura.Id] = 5,
            [evaluador1.Id] = 3,
            [evaluador2.Id] = 1
        };
        _asignacionRepository
            .ObtenerCargaPorMiembroAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult(carga));

        var comando = new AsignarEvaluadoresPostulacionCommand(
            comite.Id,
            _postulacionId,
            _fechaLimite,
            CantidadEvaluadores: 1);

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.PostulacionId.Should().Be(_postulacionId);
        resultado.Asignaciones.Should().ContainSingle();
        resultado.Asignaciones[0].MiembroComiteId.Should().Be(evaluador2.Id);
        resultado.Asignaciones[0].UsuarioEvaluadorId.Should().Be(evaluador2.UsuarioEvaluadorId);
        resultado.Asignaciones[0].Estado.Should().Be("PENDIENTE");

        await _excepcionRepository.Received(1)
            .ObtenerPorPostulacionAsync(_postulacionId, cts.Token);
        await _asignacionRepository.Received(1)
            .ObtenerCargaPorMiembroAsync(comite.Id, cts.Token);
        await _asignacionRepository.Received(1)
            .AgregarRangoAsync(
                Arg.Is<IReadOnlyCollection<AsignacionEvaluacion>>(lista => lista.Count == 1),
                cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarConEvaluacionesInsuficientesSinGuardar_Cuando_SePideCantidadUnoSinExcepcion()
    {
        var (comite, _, _) = CrearComiteConEvaluadores();
        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var comando = new AsignarEvaluadoresPostulacionCommand(
            comite.Id,
            _postulacionId,
            _fechaLimite,
            CantidadEvaluadores: 1);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluadoresInsuficientesException>()
            .Where(ex => ex.Codigo == "EVALUACIONES_INSUFICIENTES" && ex.EstadoHttp == 422);

        await _asignacionRepository.DidNotReceiveWithAnyArgs()
            .AgregarRangoAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConEvaluacionesInsuficientesSinGuardar_Cuando_SeAsignaUnEvaluadorManualSinExcepcion()
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

    private static AutorizacionExcepcionEvaluadorUnico CrearExcepcionAutorizada()
        => AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            "Único evaluador disponible para la postulación por ausencia del comité.",
            _ahora.AddHours(-1),
            _coordinadorId,
            Guid.NewGuid());

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
