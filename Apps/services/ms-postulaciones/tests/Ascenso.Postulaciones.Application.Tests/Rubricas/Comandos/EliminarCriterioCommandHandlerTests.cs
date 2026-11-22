using System.Reflection;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.EliminarCriterio;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Rubricas.Comandos;

public sealed class EliminarCriterioCommandHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000511");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000512");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000513");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaEliminacion =
        new(2026, 11, 21, 17, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IRubricaRepository _rubricaRepository = Substitute.For<IRubricaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly EliminarCriterioCommandHandler _handler;

    public EliminarCriterioCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_usuarioId);

        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(null));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new EliminarCriterioCommandHandler(
            _usuarioActual,
            _rubricaRepository,
            _unitOfWork,
            new RelojFijo(_fechaEliminacion));
    }

    [Fact]
    public async Task Debe_EliminarElCriterioYRecalcularLaSuma_Cuando_ElCriterioExiste()
    {
        using var cts = new CancellationTokenSource();
        var (rubrica, primeroId, segundoId) = CrearRubricaConDosCriterios();
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var resultado = await _handler.HandleAsync(
            new EliminarCriterioCommand(rubrica.Id, primeroId),
            cts.Token);

        resultado.Id.Should().Be(rubrica.Id);
        resultado.Estado.Should().Be("BORRADOR");
        resultado.Criterios.Should().ContainSingle();
        resultado.Criterios[0].Id.Should().Be(segundoId);
        resultado.Criterios[0].Orden.Should().Be(2);
        resultado.SumaPesos.Should().Be(40.00m);
        resultado.DiferenciaPara100.Should().Be(60.00m);

        rubrica.FechaUltimaModificacion.Should().Be(_fechaEliminacion);
        rubrica.UsuarioModificacionId.Should().Be(_usuarioId);

        await _rubricaRepository.Received(1)
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(
            new EliminarCriterioCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

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
        var accion = () => _handler.HandleAsync(
            new EliminarCriterioCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaNoEncontradaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_NO_ENCONTRADA" && excepcion.EstadoHttp == 404);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_ElCriterioNoExiste()
    {
        var (rubrica, _, _) = CrearRubricaConDosCriterios();
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var accion = () => _handler.HandleAsync(
            new EliminarCriterioCommand(rubrica.Id, Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<CriterioNoEncontradoException>()
            .Where(excepcion => excepcion.Codigo == "CRITERIO_NO_ENCONTRADO" && excepcion.EstadoHttp == 404);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Theory]
    [InlineData(EstadoRubrica.Publicada)]
    [InlineData(EstadoRubrica.Bloqueada)]
    public async Task Debe_RechazarCon409RubricaBloqueada_Cuando_LaRubricaNoEstaEnBorrador(
        EstadoRubrica estadoInmutable)
    {
        var (rubrica, criterioId) = CrearRubricaEnEstadoInmutable(estadoInmutable);
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var accion = () => _handler.HandleAsync(
            new EliminarCriterioCommand(rubrica.Id, criterioId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaBloqueadaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_BLOQUEADA" && excepcion.EstadoHttp == 409);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static (RubricaEvaluacion Rubrica, Guid PrimeroId, Guid SegundoId) CrearRubricaConDosCriterios()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Beca Excelencia",
            _fechaCreacion,
            _usuarioId);

        var primero = rubrica.AgregarCriterio(
            "Rendimiento académico",
            "Promedio ponderado",
            60.00m,
            0.00m,
            20.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioId);

        var segundo = rubrica.AgregarCriterio(
            "Situación socioeconómica",
            "Ingreso familiar",
            40.00m,
            0.00m,
            10.00m,
            _fechaCreacion.AddMinutes(10),
            _usuarioId);

        return (rubrica, primero.Id, segundo.Id);
    }

    private static (RubricaEvaluacion Rubrica, Guid CriterioId) CrearRubricaEnEstadoInmutable(
        EstadoRubrica estadoInmutable)
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Beca Excelencia",
            _fechaCreacion,
            _usuarioId);

        var criterio = rubrica.AgregarCriterio(
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

        return (rubrica, criterio.Id);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
