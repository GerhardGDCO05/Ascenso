using System.Reflection;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.PublicarRubrica;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Rubricas.Comandos;

public sealed class PublicarRubricaCommandHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000611");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000612");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000613");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaPublicacion =
        new(2026, 11, 21, 18, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IRubricaRepository _rubricaRepository = Substitute.For<IRubricaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PublicarRubricaCommandHandler _handler;

    public PublicarRubricaCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_usuarioId);

        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(null));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new PublicarRubricaCommandHandler(
            _usuarioActual,
            _rubricaRepository,
            _unitOfWork,
            new RelojFijo(_fechaPublicacion));
    }

    [Fact]
    public async Task Debe_PublicarLaRubrica_Cuando_LosPesosSumanExactamente100()
    {
        using var cts = new CancellationTokenSource();
        var rubrica = CrearRubricaConPesos(60.00m, 40.00m);
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var resultado = await _handler.HandleAsync(
            new PublicarRubricaCommand(rubrica.Id),
            cts.Token);

        resultado.Id.Should().Be(rubrica.Id);
        resultado.Estado.Should().Be("PUBLICADA");
        resultado.FechaPublicacion.Should().Be(_fechaPublicacion);
        resultado.SumaPesos.Should().Be(100.00m);
        resultado.DiferenciaPara100.Should().Be(0.00m);
        resultado.Criterios.Should().HaveCount(2);

        rubrica.UsuarioModificacionId.Should().Be(_usuarioId);
        rubrica.FechaUltimaModificacion.Should().Be(_fechaPublicacion);

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
            new PublicarRubricaCommand(Guid.NewGuid()),
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
            new PublicarRubricaCommand(Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaNoEncontradaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_NO_ENCONTRADA" && excepcion.EstadoHttp == 404);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422PesosInvalidos_Cuando_FaltanPuntosParaLlegarA100()
    {
        var rubrica = CrearRubricaConPesos(60.00m, 39.99m);
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var accion = () => _handler.HandleAsync(
            new PublicarRubricaCommand(rubrica.Id),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PesosInvalidosException>()
            .Where(excepcion =>
                excepcion.Codigo == "PESOS_INVALIDOS"
                && excepcion.EstadoHttp == 422
                && excepcion.SumaPesos == 99.99m
                && excepcion.DiferenciaPara100 == 0.01m
                && excepcion.Message.Contains("faltan 0.01", StringComparison.Ordinal));

        rubrica.Estado.Should().Be(EstadoRubrica.Borrador);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422PesosInvalidos_Cuando_LaSumaExcede100()
    {
        var rubrica = CrearRubricaConPesos(60.00m, 40.01m);
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var accion = () => _handler.HandleAsync(
            new PublicarRubricaCommand(rubrica.Id),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PesosInvalidosException>()
            .Where(excepcion =>
                excepcion.Codigo == "PESOS_INVALIDOS"
                && excepcion.EstadoHttp == 422
                && excepcion.SumaPesos == 100.01m
                && excepcion.DiferenciaPara100 == -0.01m
                && excepcion.Message.Contains("excede por 0.01", StringComparison.Ordinal));

        rubrica.Estado.Should().Be(EstadoRubrica.Borrador);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422PesosInvalidos_Cuando_LaRubricaNoTieneCriterios()
    {
        var rubrica = CrearRubricaConPesos();
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var accion = () => _handler.HandleAsync(
            new PublicarRubricaCommand(rubrica.Id),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PesosInvalidosException>()
            .Where(excepcion =>
                excepcion.Codigo == "PESOS_INVALIDOS"
                && excepcion.EstadoHttp == 422
                && excepcion.SumaPesos == 0.00m
                && excepcion.DiferenciaPara100 == 100.00m
                && excepcion.Message.Contains("faltan 100.00", StringComparison.Ordinal));

        rubrica.Estado.Should().Be(EstadoRubrica.Borrador);
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

        var accion = () => _handler.HandleAsync(
            new PublicarRubricaCommand(rubrica.Id),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaBloqueadaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_BLOQUEADA" && excepcion.EstadoHttp == 409);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static RubricaEvaluacion CrearRubricaConPesos(params decimal[] pesos)
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Beca Excelencia",
            _fechaCreacion,
            _usuarioId);

        for (var i = 0; i < pesos.Length; i++)
        {
            rubrica.AgregarCriterio(
                $"Criterio {i + 1}",
                $"Descripción {i + 1}",
                pesos[i],
                0.00m,
                10.00m,
                _fechaCreacion.AddMinutes(i + 1),
                _usuarioId);
        }

        return rubrica;
    }

    private static RubricaEvaluacion CrearRubricaEnEstadoInmutable(EstadoRubrica estadoInmutable)
    {
        var rubrica = CrearRubricaConPesos(100.00m);
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
