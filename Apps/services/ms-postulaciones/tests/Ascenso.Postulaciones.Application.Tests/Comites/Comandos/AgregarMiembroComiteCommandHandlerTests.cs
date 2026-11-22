using Ascenso.Postulaciones.Application.Comites.Comandos.AgregarMiembroComite;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Comites.Comandos;

public sealed class AgregarMiembroComiteCommandHandlerTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01966000-0000-7000-8000-000000000631");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000632");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000633");
    private static readonly Guid _jefaturaId = Guid.Parse("01966000-0000-7000-8000-000000000634");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaAsignacion =
        new(2026, 11, 23, 15, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IComiteEvaluadorRepository _comiteRepository = Substitute.For<IComiteEvaluadorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AgregarMiembroComiteCommandHandler _handler;

    public AgregarMiembroComiteCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new AgregarMiembroComiteCommandHandler(
            _usuarioActual,
            _comiteRepository,
            _unitOfWork,
            new AgregarMiembroComiteCommandValidator(),
            new RelojFijo(_fechaAsignacion));
    }

    [Theory]
    [InlineData(TipoEvaluador.Interno, "INTERNO")]
    [InlineData(TipoEvaluador.Externo, "EXTERNO")]
    public async Task Debe_AgregarEvaluadorInternoOExternoYGuardar_Cuando_ElComiteExiste(
        TipoEvaluador tipo,
        string codigoEsperado)
    {
        using var cts = new CancellationTokenSource();
        var comite = CrearComiteValido();
        var evaluadorId = Guid.NewGuid();

        _comiteRepository
            .ObtenerParaActualizarPorIdAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var comando = new AgregarMiembroComiteCommand(comite.Id, evaluadorId, tipo);

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.Id.Should().Be(comite.Id);
        resultado.Version.Should().Be(2);
        resultado.Miembros.Should().HaveCount(2);

        var miembroNuevo = resultado.Miembros.Single(miembro => miembro.UsuarioEvaluadorId == evaluadorId);
        miembroNuevo.Tipo.Should().Be(codigoEsperado);
        miembroNuevo.Rol.Should().Be("EVALUADOR");
        miembroNuevo.FechaAsignacion.Should().Be(new DateOnly(2026, 11, 23));
        miembroNuevo.FechaDesvinculacion.Should().BeNull();
        miembroNuevo.Activo.Should().BeTrue();

        await _comiteRepository.Received(1).ObtenerParaActualizarPorIdAsync(comite.Id, cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarCon404SinGuardar_Cuando_ElComiteNoExiste()
    {
        var comiteId = Guid.NewGuid();
        _comiteRepository
            .ObtenerParaActualizarPorIdAsync(comiteId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(null));

        var comando = new AgregarMiembroComiteCommand(comiteId, Guid.NewGuid(), TipoEvaluador.Interno);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ComiteNoEncontradoException>()
            .Where(excepcion => excepcion.Codigo == "COMITE_NO_ENCONTRADO" && excepcion.EstadoHttp == 404);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409SinGuardar_Cuando_ElEvaluadorYaEstaActivoEnElComite()
    {
        var comite = CrearComiteValido();
        var evaluadorId = Guid.NewGuid();
        comite.AgregarMiembro(evaluadorId, TipoEvaluador.Interno, _fechaCreacion.AddHours(1), _coordinadorId);

        _comiteRepository
            .ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var comando = new AgregarMiembroComiteCommand(comite.Id, evaluadorId, TipoEvaluador.Externo);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<MiembroComiteDuplicadoException>()
            .Where(excepcion => excepcion.Codigo == "MIEMBRO_COMITE_DUPLICADO" && excepcion.EstadoHttp == 409);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConValidacionSinConsultarRepositorio_Cuando_ElComandoEsInvalido()
    {
        var comando = new AgregarMiembroComiteCommand(Guid.Empty, Guid.NewGuid(), (TipoEvaluador)999);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();

        await _comiteRepository.DidNotReceiveWithAnyArgs().ObtenerParaActualizarPorIdAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_PropagarExcepcionConflictoConcurrencia_Cuando_GuardarCambiosFallaPorConcurrencia()
    {
        var comite = CrearComiteValido();
        _comiteRepository
            .ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ExcepcionConflictoConcurrencia()));

        var comando = new AgregarMiembroComiteCommand(comite.Id, Guid.NewGuid(), TipoEvaluador.Externo);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionConflictoConcurrencia>();
    }

    private static ComiteEvaluador CrearComiteValido()
        => ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Beca Excelencia 2026-II",
            _jefaturaId,
            _fechaCreacion,
            _coordinadorId);

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
