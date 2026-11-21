using System.Reflection;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.AgregarCriterio;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Rubricas.Comandos;

public sealed class AgregarCriterioCommandHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000311");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000312");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000313");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaModificacion =
        new(2026, 11, 21, 15, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IRubricaRepository _rubricaRepository = Substitute.For<IRubricaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AgregarCriterioCommandHandler _handler;

    public AgregarCriterioCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_usuarioId);

        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(null));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new AgregarCriterioCommandHandler(
            _usuarioActual,
            _rubricaRepository,
            _unitOfWork,
            new AgregarCriterioCommandValidator(),
            new RelojFijo(_fechaModificacion));
    }

    [Fact]
    public async Task Debe_AgregarElCriterioYRecalcularLaSuma_Cuando_LaRubricaEstaEnBorrador()
    {
        using var cts = new CancellationTokenSource();
        var rubrica = CrearRubricaEnBorrador();
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var comando = CrearComandoValido(rubrica.Id);

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.Id.Should().Be(rubrica.Id);
        resultado.Estado.Should().Be("BORRADOR");
        resultado.Criterios.Should().ContainSingle();
        resultado.Criterios[0].Id.Should().NotBeEmpty();
        resultado.Criterios[0].Nombre.Should().Be("Rendimiento académico");
        resultado.Criterios[0].Descripcion.Should().Be("Promedio ponderado acumulado");
        resultado.Criterios[0].PesoPorcentual.Should().Be(30.50m);
        resultado.Criterios[0].PuntajeMinimo.Should().Be(0.00m);
        resultado.Criterios[0].PuntajeMaximo.Should().Be(10.00m);
        resultado.Criterios[0].Orden.Should().Be(1);
        resultado.SumaPesos.Should().Be(30.50m);
        resultado.DiferenciaPara100.Should().Be(69.50m);

        rubrica.FechaUltimaModificacion.Should().Be(_fechaModificacion);
        rubrica.UsuarioModificacionId.Should().Be(_usuarioId);

        await _rubricaRepository.Received(1)
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_ElComandoEsInvalido()
    {
        var comando = CrearComandoValido(Guid.NewGuid()) with { PesoPorcentual = 0m };

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();

        await _rubricaRepository.DidNotReceiveWithAnyArgs()
            .ObtenerParaActualizarPorIdAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(CrearComandoValido(Guid.NewGuid()), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(excepcion => excepcion.Codigo == "NO_AUTENTICADO" && excepcion.EstadoHttp == 401);

        await _rubricaRepository.DidNotReceiveWithAnyArgs()
            .ObtenerParaActualizarPorIdAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_LaRubricaNoExiste()
    {
        var rubricaId = Guid.NewGuid();

        var accion = () => _handler.HandleAsync(CrearComandoValido(rubricaId), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaNoEncontradaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_NO_ENCONTRADA" && excepcion.EstadoHttp == 404);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Theory]
    [InlineData(EstadoRubrica.Publicada)]
    [InlineData(EstadoRubrica.Bloqueada)]
    public async Task Debe_RechazarCon409RubricaBloqueada_Cuando_LaRubricaNoEstaEnBorrador(
        EstadoRubrica estadoInmutable)
    {
        var rubrica = CrearRubricaEnEstadoInmutable(estadoInmutable);
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var accion = () => _handler.HandleAsync(CrearComandoValido(rubrica.Id), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaBloqueadaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_BLOQUEADA" && excepcion.EstadoHttp == 409);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static AgregarCriterioCommand CrearComandoValido(Guid rubricaId)
        => new(
            rubricaId,
            "  Rendimiento académico  ",
            "  Promedio ponderado acumulado  ",
            30.50m,
            0.00m,
            10.00m);

    private static RubricaEvaluacion CrearRubricaEnBorrador()
        => RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Beca Excelencia",
            _fechaCreacion,
            _usuarioId);

    private static RubricaEvaluacion CrearRubricaEnEstadoInmutable(EstadoRubrica estadoInmutable)
    {
        var rubrica = CrearRubricaEnBorrador();
        rubrica.AgregarCriterio(
            "Criterio único",
            "Cubre el 100%",
            100.00m,
            0.00m,
            10.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioId);
        rubrica.Publicar(_fechaCreacion.AddMinutes(10), _usuarioId);

        if (estadoInmutable == EstadoRubrica.Bloqueada)
        {
            typeof(RubricaEvaluacion)
                .GetProperty(nameof(RubricaEvaluacion.Estado), BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(rubrica, EstadoRubrica.Bloqueada);
        }

        return rubrica;
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
