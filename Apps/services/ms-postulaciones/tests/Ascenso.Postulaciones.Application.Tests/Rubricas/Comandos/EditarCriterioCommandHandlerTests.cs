using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.EditarCriterio;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Rubricas.Comandos;

public sealed class EditarCriterioCommandHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000411");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000412");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000413");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaEdicion =
        new(2026, 11, 21, 16, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IRubricaRepository _rubricaRepository = Substitute.For<IRubricaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly EditarCriterioCommandHandler _handler;

    public EditarCriterioCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_usuarioId);

        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(null));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new EditarCriterioCommandHandler(
            _usuarioActual,
            _rubricaRepository,
            _unitOfWork,
            new EditarCriterioCommandValidator(),
            new RelojFijo(_fechaEdicion));
    }

    [Fact]
    public async Task Debe_EditarElCriterioYRecalcularLaSuma_Cuando_LosDatosSonValidos()
    {
        using var cts = new CancellationTokenSource();
        var (rubrica, criterioId) = CrearRubricaConCriterio();
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var comando = CrearComandoValido(rubrica.Id, criterioId);

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.Id.Should().Be(rubrica.Id);
        resultado.Estado.Should().Be("BORRADOR");
        resultado.Criterios.Should().ContainSingle();
        resultado.Criterios[0].Id.Should().Be(criterioId);
        resultado.Criterios[0].Nombre.Should().Be("Rendimiento académico actualizado");
        resultado.Criterios[0].Descripcion.Should().Be("Promedio ponderado general");
        resultado.Criterios[0].PesoPorcentual.Should().Be(65.25m);
        resultado.Criterios[0].PuntajeMinimo.Should().Be(1.00m);
        resultado.Criterios[0].PuntajeMaximo.Should().Be(25.00m);
        resultado.Criterios[0].Orden.Should().Be(1);
        resultado.SumaPesos.Should().Be(65.25m);
        resultado.DiferenciaPara100.Should().Be(34.75m);

        rubrica.FechaUltimaModificacion.Should().Be(_fechaEdicion);
        rubrica.UsuarioModificacionId.Should().Be(_usuarioId);

        await _rubricaRepository.Received(1)
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_ElComandoEsInvalido()
    {
        var comando = CrearComandoValido(Guid.NewGuid(), Guid.NewGuid()) with
        {
            PuntajeMinimo = 10.00m,
            PuntajeMaximo = 10.00m
        };

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

        var accion = () => _handler.HandleAsync(
            CrearComandoValido(Guid.NewGuid(), Guid.NewGuid()),
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
            CrearComandoValido(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaNoEncontradaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_NO_ENCONTRADA" && excepcion.EstadoHttp == 404);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_ElCriterioNoExiste()
    {
        var (rubrica, _) = CrearRubricaConCriterio();
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var accion = () => _handler.HandleAsync(
            CrearComandoValido(rubrica.Id, Guid.NewGuid()),
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
            CrearComandoValido(rubrica.Id, criterioId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaBloqueadaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_BLOQUEADA" && excepcion.EstadoHttp == 409);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static EditarCriterioCommand CrearComandoValido(Guid rubricaId, Guid criterioId)
        => new(
            rubricaId,
            criterioId,
            "  Rendimiento académico actualizado  ",
            "  Promedio ponderado general  ",
            65.25m,
            1.00m,
            25.00m);

    private static (RubricaEvaluacion Rubrica, Guid CriterioId) CrearRubricaConCriterio()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Beca Excelencia",
            _fechaCreacion,
            _usuarioId);

        var criterio = rubrica.AgregarCriterio(
            "Rendimiento académico",
            "Promedio ponderado",
            40.00m,
            0.00m,
            20.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioId);

        return (rubrica, criterio.Id);
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
            rubrica.Bloquear(
                MotivoBloqueoRubrica.InicioEvaluacion,
                _fechaCreacion.AddMinutes(15),
                UsuarioSistema.Id);
        }

        return (rubrica, criterio.Id);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
