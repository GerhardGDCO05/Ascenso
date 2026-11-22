using Ascenso.Postulaciones.Application.Comites.Comandos.DesvincularMiembroComite;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Comites.Comandos;

public sealed class DesvincularMiembroComiteCommandHandlerTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01966000-0000-7000-8000-000000000651");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000652");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000653");
    private static readonly Guid _jefaturaId = Guid.Parse("01966000-0000-7000-8000-000000000654");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaDesvinculacion =
        new(2026, 11, 25, 16, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IComiteEvaluadorRepository _comiteRepository = Substitute.For<IComiteEvaluadorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DesvincularMiembroComiteCommandHandler _handler;

    public DesvincularMiembroComiteCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new DesvincularMiembroComiteCommandHandler(
            _usuarioActual,
            _comiteRepository,
            _unitOfWork,
            new DesvincularMiembroComiteCommandValidator(),
            new RelojFijo(_fechaDesvinculacion));
    }

    [Fact]
    public async Task Debe_DesvincularEvaluadorYGuardar_Cuando_EstaActivo()
    {
        using var cts = new CancellationTokenSource();
        var comite = CrearComiteValido();
        var miembro = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaCreacion.AddHours(1),
            _coordinadorId);

        _comiteRepository
            .ObtenerParaActualizarPorIdAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var comando = new DesvincularMiembroComiteCommand(comite.Id, miembro.Id);

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.Version.Should().Be(3);
        var miembroDto = resultado.Miembros.Single(item => item.Id == miembro.Id);
        miembroDto.Activo.Should().BeFalse();
        miembroDto.FechaDesvinculacion.Should().Be(new DateOnly(2026, 11, 25));

        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_SerIdempotenteSinLlamarAGuardarCambios_Cuando_ElMiembroYaEstaDesvinculado()
    {
        var comite = CrearComiteValido();
        var miembro = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacion.AddHours(1),
            _coordinadorId);
        comite.DesvincularMiembro(miembro.Id, _fechaCreacion.AddDays(1), _coordinadorId);
        var versionPrevia = comite.Version;

        _comiteRepository
            .ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var comando = new DesvincularMiembroComiteCommand(comite.Id, miembro.Id);

        var resultado = await _handler.HandleAsync(comando, CancellationToken.None);

        resultado.Version.Should().Be(versionPrevia);
        var miembroDto = resultado.Miembros.Single(item => item.Id == miembro.Id);
        miembroDto.Activo.Should().BeFalse();
        miembroDto.FechaDesvinculacion.Should().Be(new DateOnly(2026, 11, 23));

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon404SinGuardar_Cuando_ElComiteNoExiste()
    {
        var comiteId = Guid.NewGuid();
        _comiteRepository
            .ObtenerParaActualizarPorIdAsync(comiteId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(null));

        var comando = new DesvincularMiembroComiteCommand(comiteId, Guid.NewGuid());

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ComiteNoEncontradoException>()
            .Where(excepcion => excepcion.Codigo == "COMITE_NO_ENCONTRADO" && excepcion.EstadoHttp == 404);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422SinGuardar_Cuando_SeIntentaDesvincularALaJefatura()
    {
        var comite = CrearComiteValido();
        var jefatura = comite.Miembros.Single();

        _comiteRepository
            .ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var comando = new DesvincularMiembroComiteCommand(comite.Id, jefatura.Id);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<JefaturaNoDesvinculableException>()
            .Where(excepcion => excepcion.Codigo == "JEFATURA_NO_DESVINCULABLE" && excepcion.EstadoHttp == 422);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon404SinGuardar_Cuando_ElMiembroNoPerteneceAlComite()
    {
        var comite = CrearComiteValido();

        _comiteRepository
            .ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var comando = new DesvincularMiembroComiteCommand(comite.Id, Guid.NewGuid());

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<MiembroComiteNoEncontradoException>()
            .Where(excepcion => excepcion.Codigo == "MIEMBRO_COMITE_NO_ENCONTRADO" && excepcion.EstadoHttp == 404);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_LosIdentificadoresEstanVacios()
    {
        var comando = new DesvincularMiembroComiteCommand(Guid.Empty, Guid.Empty);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();

        await _comiteRepository.DidNotReceiveWithAnyArgs().ObtenerParaActualizarPorIdAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_PropagarExcepcionConflictoConcurrencia_Cuando_GuardarCambiosFallaPorConcurrencia()
    {
        var comite = CrearComiteValido();
        var miembro = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaCreacion.AddHours(1),
            _coordinadorId);

        _comiteRepository
            .ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ExcepcionConflictoConcurrencia()));

        var comando = new DesvincularMiembroComiteCommand(comite.Id, miembro.Id);

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
