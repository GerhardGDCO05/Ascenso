using System.Reflection;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.RevisarDiscrepanciaConsolidacion;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.Comandos;

public sealed class RevisarDiscrepanciaConsolidacionCommandHandlerTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000002301");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000002302");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000002303");
    private static readonly DateTimeOffset _ahora = new(2026, 11, 26, 14, 30, 0, TimeSpan.Zero);
    private const string ComentarioValido = "Se contrastaron las dos evaluaciones y se validó la diferencia de criterio.";

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IResultadoConsolidadoRepository _resultadoRepository = Substitute.For<IResultadoConsolidadoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RevisarDiscrepanciaConsolidacionCommandHandler _handler;

    public RevisarDiscrepanciaConsolidacionCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(null));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new RevisarDiscrepanciaConsolidacionCommandHandler(
            _usuarioActual,
            _resultadoRepository,
            _unitOfWork,
            new RevisarDiscrepanciaConsolidacionCommandValidator(),
            new RelojFijo(_ahora));
    }

    [Fact]
    public async Task Debe_RevisarDiscrepanciaYGuardarConUsuarioYFechaUtc_Cuando_SolicitudEsValida()
    {
        using var cts = new CancellationTokenSource();
        var agregado = CrearResultado(alerta: true);
        _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(agregado));

        var comando = new RevisarDiscrepanciaConsolidacionCommand(_postulacionId, $"  {ComentarioValido}  ");

        var resultado = await _handler.HandleAsync(comando, cts.Token);

        resultado.DiscrepanciaRevisada.Should().BeTrue();
        resultado.ComentarioRevisionDiscrepancia.Should().Be(ComentarioValido);
        resultado.RevisadoPorUsuarioId.Should().Be(_coordinadorId);
        resultado.FechaRevisionDiscrepancia.Should().Be(_ahora);
        resultado.Version.Should().Be(2);

        await _resultadoRepository.Received(1)
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, cts.Token);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_FallarValidacion_Cuando_PostulacionIdEsVacio()
    {
        var comando = new RevisarDiscrepanciaConsolidacionCommand(Guid.Empty, ComentarioValido);

        var accion = () => _handler.HandleAsync(comando);

        await accion.Should().ThrowAsync<ValidationException>();
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("         ")]
    [InlineData("Corto")]
    [InlineData("   123456789   ")]
    public async Task Debe_FallarValidacion_Cuando_ComentarioEsVacioOMenorADiezCaracteres(string comentarioInvalido)
    {
        var comando = new RevisarDiscrepanciaConsolidacionCommand(_postulacionId, comentarioInvalido);

        var accion = () => _handler.HandleAsync(comando);

        var excepcion = await accion.Should().ThrowAsync<ValidationException>();
        excepcion.Which.Errors.Should()
            .Contain(e => e.ErrorCode == ComentarioRevisionDiscrepanciaInvalidoException.CodigoError);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_FallarValidacion_Cuando_ComentarioSuperaMilCaracteres()
    {
        var comando = new RevisarDiscrepanciaConsolidacionCommand(
            _postulacionId,
            new string('A', ResultadoConsolidadoRanking.LongitudMaximaComentarioRevision + 1));

        var accion = () => _handler.HandleAsync(comando);

        var excepcion = await accion.Should().ThrowAsync<ValidationException>();
        excepcion.Which.Errors.Should()
            .Contain(e => e.ErrorCode == ComentarioRevisionDiscrepanciaInvalidoException.CodigoError);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConNoAutenticado_Cuando_UsuarioNoEstaAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(
            new RevisarDiscrepanciaConsolidacionCommand(_postulacionId, ComentarioValido));

        await accion.Should().ThrowAsync<NoAutenticadoException>();
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_NoExisteResultadoConsolidado()
    {
        var accion = () => _handler.HandleAsync(
            new RevisarDiscrepanciaConsolidacionCommand(_postulacionId, ComentarioValido));

        await accion.Should()
            .ThrowAsync<ResultadoConsolidadoNoEncontradoException>()
            .Where(ex => ex.Codigo == "RESULTADO_CONSOLIDADO_NO_ENCONTRADO" && ex.EstadoHttp == 404);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422_Cuando_NoHayAlertaDeDiscrepancia()
    {
        var agregado = CrearResultado(alerta: false);
        _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(agregado));

        var accion = () => _handler.HandleAsync(
            new RevisarDiscrepanciaConsolidacionCommand(_postulacionId, ComentarioValido));

        await accion.Should()
            .ThrowAsync<DiscrepanciaNoDetectadaException>()
            .Where(ex => ex.Codigo == "DISCREPANCIA_NO_DETECTADA" && ex.EstadoHttp == 422);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409_Cuando_LaDiscrepanciaYaFueRevisada()
    {
        var agregado = CrearResultado(alerta: true);
        agregado.MarcarDiscrepanciaRevisada("Revisión previa registrada.", _ahora.AddHours(-1), _coordinadorId);

        _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(agregado));

        var accion = () => _handler.HandleAsync(
            new RevisarDiscrepanciaConsolidacionCommand(_postulacionId, ComentarioValido));

        await accion.Should()
            .ThrowAsync<DiscrepanciaYaRevisadaException>()
            .Where(ex => ex.Codigo == "DISCREPANCIA_YA_REVISADA" && ex.EstadoHttp == 409);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409_Cuando_ElRankingYaEstaConfirmado()
    {
        var agregado = CrearResultado(alerta: true);
        FijarEstadoRanking(agregado, EstadoRanking.Confirmado);

        _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(agregado));

        var accion = () => _handler.HandleAsync(
            new RevisarDiscrepanciaConsolidacionCommand(_postulacionId, ComentarioValido));

        await accion.Should()
            .ThrowAsync<RankingYaConfirmadoException>()
            .Where(ex => ex.Codigo == "RANKING_YA_CONFIRMADO" && ex.EstadoHttp == 409);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static ResultadoConsolidadoRanking CrearResultado(bool alerta)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            _postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 80.00m,
            diferenciaEntreEvaluadores: alerta ? 25.00m : 10.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: alerta);

        return ResultadoConsolidadoRanking.Crear(calculo, _ahora.AddHours(-2), UsuarioSistema.Id);
    }

    private static void FijarEstadoRanking(ResultadoConsolidadoRanking resultado, EstadoRanking estado)
    {
        var propiedad = typeof(ResultadoConsolidadoRanking)
            .GetProperty(nameof(ResultadoConsolidadoRanking.EstadoConfirmacionRanking), BindingFlags.Instance | BindingFlags.Public);

        propiedad.Should().NotBeNull();
        propiedad!.SetValue(resultado, estado);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
