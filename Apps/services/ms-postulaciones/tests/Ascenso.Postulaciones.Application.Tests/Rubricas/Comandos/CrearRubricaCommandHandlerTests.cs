using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.CrearRubrica;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Rubricas.Comandos;

public sealed class CrearRubricaCommandHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000211");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000212");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000213");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IRubricaRepository _rubricaRepository = Substitute.For<IRubricaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CrearRubricaCommandHandler _handler;

    public CrearRubricaCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_usuarioId);

        _rubricaRepository
            .ExistePorProgramaYConvocatoriaAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _rubricaRepository
            .AgregarAsync(Arg.Any<RubricaEvaluacion>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new CrearRubricaCommandHandler(
            _usuarioActual,
            _rubricaRepository,
            _unitOfWork,
            new CrearRubricaCommandValidator(),
            new RelojFijo(_fechaCreacion));
    }

    [Fact]
    public async Task Debe_CrearLaRubricaEnBorrador_Cuando_LosDatosSonValidos()
    {
        using var cts = new CancellationTokenSource();
        var comando = CrearComandoValido();

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.Id.Should().NotBeEmpty();
        resultado.ProgramaBecaId.Should().Be(_programaBecaId);
        resultado.ConvocatoriaId.Should().Be(_convocatoriaId);
        resultado.Nombre.Should().Be("Rúbrica Beca Excelencia 2026-II");
        resultado.Estado.Should().Be("BORRADOR");
        resultado.FechaPublicacion.Should().BeNull();
        resultado.Criterios.Should().BeEmpty();
        resultado.SumaPesos.Should().Be(0.00m);
        resultado.DiferenciaPara100.Should().Be(100.00m);

        await _rubricaRepository.Received(1)
            .ExistePorProgramaYConvocatoriaAsync(_programaBecaId, _convocatoriaId, cts.Token);
        await _rubricaRepository.Received(1)
            .AgregarAsync(
                Arg.Is<RubricaEvaluacion>(rubrica =>
                    rubrica.ProgramaBecaId == _programaBecaId
                    && rubrica.ConvocatoriaId == _convocatoriaId
                    && rubrica.Nombre == "Rúbrica Beca Excelencia 2026-II"
                    && rubrica.Estado == EstadoRubrica.Borrador
                    && rubrica.UsuarioCreacionId == _usuarioId
                    && rubrica.FechaCreacion == _fechaCreacion),
                cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_ElComandoEsInvalido()
    {
        var comando = CrearComandoValido() with { Nombre = "   " };

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();

        await _rubricaRepository.DidNotReceiveWithAnyArgs()
            .ExistePorProgramaYConvocatoriaAsync(default, default, default);
        await _rubricaRepository.DidNotReceiveWithAnyArgs()
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

        await _rubricaRepository.DidNotReceiveWithAnyArgs()
            .ExistePorProgramaYConvocatoriaAsync(default, default, default);
        await _rubricaRepository.DidNotReceiveWithAnyArgs()
            .AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409RubricaDuplicada_Cuando_YaExisteRubricaParaProgramaYConvocatoria()
    {
        _rubricaRepository
            .ExistePorProgramaYConvocatoriaAsync(_programaBecaId, _convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaDuplicadaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_DUPLICADA" && excepcion.EstadoHttp == 409);

        await _rubricaRepository.DidNotReceiveWithAnyArgs()
            .AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static CrearRubricaCommand CrearComandoValido()
        => new(
            _programaBecaId,
            _convocatoriaId,
            "  Rúbrica Beca Excelencia 2026-II  ");

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
