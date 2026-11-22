using Ascenso.Postulaciones.Application.Comites.Comandos.CrearComiteEvaluador;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Comites.Comandos;

public sealed class CrearComiteEvaluadorCommandHandlerTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01966000-0000-7000-8000-000000000611");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000612");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000613");
    private static readonly Guid _jefaturaId = Guid.Parse("01966000-0000-7000-8000-000000000614");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 22, 14, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IComiteEvaluadorRepository _comiteRepository = Substitute.For<IComiteEvaluadorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CrearComiteEvaluadorCommandHandler _handler;

    public CrearComiteEvaluadorCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _comiteRepository
            .ExistePorProgramaYConvocatoriaAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _comiteRepository
            .AgregarAsync(Arg.Any<ComiteEvaluador>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new CrearComiteEvaluadorCommandHandler(
            _usuarioActual,
            _comiteRepository,
            _unitOfWork,
            new CrearComiteEvaluadorCommandValidator(),
            new RelojFijo(_fechaCreacion));
    }

    [Fact]
    public async Task Debe_CrearComiteConJefaturaYGuardarUnaVez_Cuando_LosDatosSonValidos()
    {
        using var cts = new CancellationTokenSource();
        var comando = CrearComandoValido();

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.Id.Should().NotBeEmpty();
        resultado.ProgramaBecaId.Should().Be(_programaBecaId);
        resultado.ConvocatoriaId.Should().Be(_convocatoriaId);
        resultado.Nombre.Should().Be("Comité Beca Excelencia 2026-II");
        resultado.JefaturaUsuarioId.Should().Be(_jefaturaId);
        resultado.FechaConformacion.Should().Be(new DateOnly(2026, 11, 22));
        resultado.ConformadoPorUsuarioId.Should().Be(_coordinadorId);
        resultado.Activo.Should().BeTrue();
        resultado.Version.Should().Be(1);

        resultado.Miembros.Should().ContainSingle();
        resultado.Miembros[0].UsuarioEvaluadorId.Should().Be(_jefaturaId);
        resultado.Miembros[0].Tipo.Should().Be("INTERNO");
        resultado.Miembros[0].Rol.Should().Be("JEFATURA");
        resultado.Miembros[0].Activo.Should().BeTrue();

        await _comiteRepository.Received(1)
            .ExistePorProgramaYConvocatoriaAsync(_programaBecaId, _convocatoriaId, cts.Token);
        await _comiteRepository.Received(1)
            .AgregarAsync(
                Arg.Is<ComiteEvaluador>(comite =>
                    comite.ProgramaBecaId == _programaBecaId
                    && comite.ConvocatoriaId == _convocatoriaId
                    && comite.Nombre == "Comité Beca Excelencia 2026-II"
                    && comite.JefaturaUsuarioId == _jefaturaId
                    && comite.ConformadoPorUsuarioId == _coordinadorId
                    && comite.FechaCreacion == _fechaCreacion),
                cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_ElComandoEsInvalido()
    {
        var comando = CrearComandoValido() with { Nombre = "   " };

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();

        await _comiteRepository.DidNotReceiveWithAnyArgs()
            .ExistePorProgramaYConvocatoriaAsync(default, default, default);
        await _comiteRepository.DidNotReceiveWithAnyArgs()
            .AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(excepcion => excepcion.Codigo == "NO_AUTENTICADO" && excepcion.EstadoHttp == 401);

        await _comiteRepository.DidNotReceiveWithAnyArgs()
            .ExistePorProgramaYConvocatoriaAsync(default, default, default);
        await _comiteRepository.DidNotReceiveWithAnyArgs()
            .AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409ComiteDuplicadoSinGuardar_Cuando_YaExisteComite()
    {
        _comiteRepository
            .ExistePorProgramaYConvocatoriaAsync(_programaBecaId, _convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ComiteDuplicadoException>()
            .Where(excepcion => excepcion.Codigo == "COMITE_DUPLICADO" && excepcion.EstadoHttp == 409);

        await _comiteRepository.DidNotReceiveWithAnyArgs()
            .AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_PropagarCancelacion_Cuando_ElTokenEstaCancelado()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _comiteRepository
            .ExistePorProgramaYConvocatoriaAsync(_programaBecaId, _convocatoriaId, cts.Token)
            .Returns(Task.FromCanceled<bool>(cts.Token));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static CrearComiteEvaluadorCommand CrearComandoValido()
        => new(
            _programaBecaId,
            _convocatoriaId,
            "  Comité Beca Excelencia 2026-II  ",
            _jefaturaId);

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
