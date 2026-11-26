using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerExcepcionEvaluadorUnico;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Consultas;

public sealed class ObtenerExcepcionEvaluadorUnicoQueryHandlerTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000801");
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000802");
    private static readonly Guid _correlacionId = Guid.Parse("01967000-0000-7000-8000-000000000803");
    private static readonly DateTimeOffset _fechaAutorizacion =
        new(2026, 11, 24, 17, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IExcepcionEvaluadorUnicoRepository _excepcionRepository =
        Substitute.For<IExcepcionEvaluadorUnicoRepository>();
    private readonly ObtenerExcepcionEvaluadorUnicoQueryHandler _handler;

    public ObtenerExcepcionEvaluadorUnicoQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _excepcionRepository
            .ObtenerPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(null));

        _handler = new ObtenerExcepcionEvaluadorUnicoQueryHandler(
            _usuarioActual,
            _excepcionRepository,
            new ObtenerExcepcionEvaluadorUnicoQueryValidator());
    }

    [Fact]
    public async Task Debe_DevolverDtoMapeado_Cuando_ExisteExcepcionParaLaPostulacion()
    {
        using var cts = new CancellationTokenSource();
        var autorizacion = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            "Comité con un único evaluador disponible por ausencia justificada.",
            _fechaAutorizacion,
            _coordinadorId,
            _correlacionId);

        _excepcionRepository
            .ObtenerPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(autorizacion));

        var resultado = await _handler.HandleAsync(
            new ObtenerExcepcionEvaluadorUnicoQuery(_postulacionId),
            cts.Token);

        resultado.Id.Should().Be(autorizacion.Id);
        resultado.PostulacionId.Should().Be(_postulacionId);
        resultado.Justificacion.Should().Be(autorizacion.Justificacion);
        resultado.UsuarioAutorizadorId.Should().Be(_coordinadorId);
        resultado.FechaAutorizacion.Should().Be(_fechaAutorizacion);
        resultado.FechaAutorizacion.Offset.Should().Be(TimeSpan.Zero);

        await _excepcionRepository.Received(1)
            .ObtenerPorPostulacionAsync(_postulacionId, cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_NoExisteExcepcionParaLaPostulacion()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerExcepcionEvaluadorUnicoQuery(_postulacionId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ExcepcionEvaluadorUnicoNoEncontradaException>()
            .Where(ex => ex.Codigo == "EXCEPCION_NO_ENCONTRADA" && ex.EstadoHttp == 404);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_PostulacionIdEstaVacio()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerExcepcionEvaluadorUnicoQuery(Guid.Empty),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();
        await _excepcionRepository.DidNotReceiveWithAnyArgs()
            .ObtenerPorPostulacionAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(
            new ObtenerExcepcionEvaluadorUnicoQuery(_postulacionId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(ex => ex.Codigo == "NO_AUTENTICADO" && ex.EstadoHttp == 401);
    }

    [Fact]
    public async Task Debe_PropagarCancelacion_Cuando_ElTokenEstaCancelado()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _excepcionRepository
            .ObtenerPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromCanceled<AutorizacionExcepcionEvaluadorUnico?>(cts.Token));

        var accion = () => _handler.HandleAsync(
            new ObtenerExcepcionEvaluadorUnicoQuery(_postulacionId),
            cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }
}
