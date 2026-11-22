using Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Rubricas.Comandos;

public sealed class BloquearRubricaCommandHandlerTests
{
    private static readonly Guid _usuarioCreadorId = Guid.Parse("01966000-0000-7000-8000-000000000711");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000712");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000713");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaPublicacion =
        new(2026, 11, 21, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaBloqueo =
        new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    private readonly IRubricaRepository _rubricaRepository = Substitute.For<IRubricaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BloquearRubricaCommandHandler _handler;

    public BloquearRubricaCommandHandlerTests()
    {
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(null));
        _rubricaRepository
            .ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(null));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new BloquearRubricaCommandHandler(
            _rubricaRepository,
            _unitOfWork,
            new RelojFijo(_fechaBloqueo));
    }

    [Theory]
    [InlineData(MotivoBloqueoRubrica.InicioEvaluacion, "INICIO_EVALUACION")]
    [InlineData(MotivoBloqueoRubrica.PrimeraCalificacion, "PRIMERA_CALIFICACION")]
    public async Task Debe_BloquearLaRubricaYGuardarUnaVez_Cuando_EstaPublicada(
        MotivoBloqueoRubrica motivo,
        string codigoMotivoEsperado)
    {
        using var cts = new CancellationTokenSource();
        var rubrica = CrearRubricaPublicada();
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var resultado = await _handler.HandleAsync(
            new BloquearRubricaCommand(rubrica.Id, motivo),
            cts.Token);

        resultado.Id.Should().Be(rubrica.Id);
        resultado.Estado.Should().Be("BLOQUEADA");
        resultado.MotivoBloqueo.Should().Be(codigoMotivoEsperado);
        resultado.FechaBloqueo.Should().Be(_fechaBloqueo);
        resultado.SoloLectura.Should().BeTrue();

        rubrica.UsuarioModificacionId.Should().Be(UsuarioSistema.Id);
        rubrica.FechaUltimaModificacion.Should().Be(_fechaBloqueo);

        await _rubricaRepository.Received(1)
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token);
        await _rubricaRepository.DidNotReceiveWithAnyArgs()
            .ObtenerPorIdAsync(default, default);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarCon404SinGuardar_Cuando_LaRubricaNoExiste()
    {
        var accion = () => _handler.HandleAsync(
            new BloquearRubricaCommand(Guid.NewGuid(), MotivoBloqueoRubrica.InicioEvaluacion),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaNoEncontradaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_NO_ENCONTRADA" && excepcion.EstadoHttp == 404);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_SerIdempotenteSinGuardar_Cuando_LaRubricaYaEstaBloqueada()
    {
        using var cts = new CancellationTokenSource();
        var fechaOriginal = _fechaPublicacion.AddHours(1);
        var rubrica = CrearRubricaPublicada();
        rubrica.Bloquear(MotivoBloqueoRubrica.InicioEvaluacion, fechaOriginal, UsuarioSistema.Id);
        var versionOriginal = rubrica.Version;

        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var resultado = await _handler.HandleAsync(
            new BloquearRubricaCommand(rubrica.Id, MotivoBloqueoRubrica.PrimeraCalificacion),
            cts.Token);

        resultado.Estado.Should().Be("BLOQUEADA");
        resultado.MotivoBloqueo.Should().Be("INICIO_EVALUACION");
        resultado.FechaBloqueo.Should().Be(fechaOriginal);
        resultado.SoloLectura.Should().BeTrue();
        rubrica.Version.Should().Be(versionOriginal);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409RubricaNoPublicadaSinGuardar_Cuando_EstaEnBorrador()
    {
        var rubrica = CrearRubricaEnBorrador();
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var accion = () => _handler.HandleAsync(
            new BloquearRubricaCommand(rubrica.Id, MotivoBloqueoRubrica.InicioEvaluacion),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaNoPublicadaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_NO_PUBLICADA" && excepcion.EstadoHttp == 409);

        rubrica.Estado.Should().Be(EstadoRubrica.Borrador);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarSinGuardar_Cuando_ElMotivoNoEstaDefinido()
    {
        var rubrica = CrearRubricaPublicada();
        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var accion = () => _handler.HandleAsync(
            new BloquearRubricaCommand(rubrica.Id, (MotivoBloqueoRubrica)999),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ArgumentOutOfRangeException>();

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_ResponderConExitoIdempotente_Cuando_HayConflictoYLaRelecturaEstaBloqueada()
    {
        using var cts = new CancellationTokenSource();
        var rubricaObsoleta = CrearRubricaPublicada();
        var fechaGanadora = _fechaBloqueo.AddSeconds(-5);
        var rubricaGanadora = CrearRubricaPublicada();
        rubricaGanadora.Bloquear(
            MotivoBloqueoRubrica.InicioEvaluacion,
            fechaGanadora,
            UsuarioSistema.Id);

        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubricaObsoleta.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubricaObsoleta));
        _unitOfWork
            .GuardarCambiosAsync(cts.Token)
            .Returns<Task<int>>(_ => throw new ExcepcionConflictoConcurrencia());
        _rubricaRepository
            .ObtenerPorIdAsync(rubricaObsoleta.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubricaGanadora));

        var resultado = await _handler.HandleAsync(
            new BloquearRubricaCommand(rubricaObsoleta.Id, MotivoBloqueoRubrica.PrimeraCalificacion),
            cts.Token);

        resultado.Estado.Should().Be("BLOQUEADA");
        resultado.MotivoBloqueo.Should().Be("INICIO_EVALUACION");
        resultado.FechaBloqueo.Should().Be(fechaGanadora);
        resultado.SoloLectura.Should().BeTrue();

        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
        await _rubricaRepository.Received(1).ObtenerPorIdAsync(rubricaObsoleta.Id, cts.Token);
    }

    [Fact]
    public async Task Debe_PropagarElConflictoDeConcurrencia_Cuando_LaRelecturaNoEstaBloqueada()
    {
        using var cts = new CancellationTokenSource();
        var rubricaObsoleta = CrearRubricaPublicada();
        var rubricaReleidaPublicada = CrearRubricaPublicada();
        var conflictoOriginal = new ExcepcionConflictoConcurrencia("Conflicto ajeno al bloqueo.");

        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubricaObsoleta.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubricaObsoleta));
        _unitOfWork
            .GuardarCambiosAsync(cts.Token)
            .Returns<Task<int>>(_ => throw conflictoOriginal);
        _rubricaRepository
            .ObtenerPorIdAsync(rubricaObsoleta.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubricaReleidaPublicada));

        var accion = () => _handler.HandleAsync(
            new BloquearRubricaCommand(rubricaObsoleta.Id, MotivoBloqueoRubrica.InicioEvaluacion),
            cts.Token);

        var lanzada = await accion.Should().ThrowAsync<ExcepcionConflictoConcurrencia>();
        lanzada.Which.Should().BeSameAs(conflictoOriginal);

        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
        await _rubricaRepository.Received(1).ObtenerPorIdAsync(rubricaObsoleta.Id, cts.Token);
    }

    [Fact]
    public async Task Debe_PropagarElConflictoDeConcurrencia_Cuando_LaRelecturaNoEncuentraLaRubrica()
    {
        using var cts = new CancellationTokenSource();
        var rubricaObsoleta = CrearRubricaPublicada();
        var conflictoOriginal = new ExcepcionConflictoConcurrencia();

        _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubricaObsoleta.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubricaObsoleta));
        _unitOfWork
            .GuardarCambiosAsync(cts.Token)
            .Returns<Task<int>>(_ => throw conflictoOriginal);
        _rubricaRepository
            .ObtenerPorIdAsync(rubricaObsoleta.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(null));

        var accion = () => _handler.HandleAsync(
            new BloquearRubricaCommand(rubricaObsoleta.Id, MotivoBloqueoRubrica.InicioEvaluacion),
            cts.Token);

        var lanzada = await accion.Should().ThrowAsync<ExcepcionConflictoConcurrencia>();
        lanzada.Which.Should().BeSameAs(conflictoOriginal);

        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
        await _rubricaRepository.Received(1).ObtenerPorIdAsync(rubricaObsoleta.Id, cts.Token);
    }

    private static RubricaEvaluacion CrearRubricaEnBorrador()
        => RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Beca Excelencia",
            _fechaCreacion,
            _usuarioCreadorId);

    private static RubricaEvaluacion CrearRubricaPublicada()
    {
        var rubrica = CrearRubricaEnBorrador();
        rubrica.AgregarCriterio(
            "Mérito académico",
            "Promedio ponderado",
            100.00m,
            0.00m,
            20.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioCreadorId);
        rubrica.Publicar(_fechaPublicacion, _usuarioCreadorId);

        return rubrica;
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
