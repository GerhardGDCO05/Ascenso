using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AutorizarExcepcionEvaluadorUnico;
using Ascenso.Postulaciones.Application.Consolidacion.Interfaces;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Comandos;

public sealed class AutorizarExcepcionEvaluadorUnicoConsolidacionTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01968400-0000-7000-8000-000000000701");
    private static readonly Guid _postulacionId = Guid.Parse("01968400-0000-7000-8000-000000000702");
    private static readonly DateTimeOffset _ahora = new(2026, 11, 26, 16, 0, 0, TimeSpan.Zero);
    private const string JustificacionValida = "Único evaluador especializado disponible en la institución.";

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IExcepcionEvaluadorUnicoRepository _excepcionRepository =
        Substitute.For<IExcepcionEvaluadorUnicoRepository>();
    private readonly IAsignacionEvaluacionRepository _asignacionRepository =
        Substitute.For<IAsignacionEvaluacionRepository>();
    private readonly IConsolidadorPostulacionEvaluacion _consolidadorPostulacion =
        Substitute.For<IConsolidadorPostulacionEvaluacion>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AutorizarExcepcionEvaluadorUnicoCommandHandler _handler;

    public AutorizarExcepcionEvaluadorUnicoConsolidacionTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _excepcionRepository
            .ObtenerPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(null));
        _excepcionRepository
            .AgregarAsync(Arg.Any<AutorizacionExcepcionEvaluadorUnico>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _asignacionRepository
            .ListarVigentesPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AsignacionEvaluacion>>([]));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new AutorizarExcepcionEvaluadorUnicoCommandHandler(
            _usuarioActual,
            _excepcionRepository,
            _asignacionRepository,
            _consolidadorPostulacion,
            _unitOfWork,
            new AutorizarExcepcionEvaluadorUnicoCommandValidator(),
            new RelojFijo(_ahora));
    }

    [Fact]
    public async Task Debe_InvocarConsolidadorDespuesDeGuardarCambios_Cuando_AutorizacionEsExitosa()
    {
        using var cts = new CancellationTokenSource();
        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(_postulacionId, JustificacionValida);

        await _handler.HandleAsync(comando, cts.Token);

        Received.InOrder(() =>
        {
            _unitOfWork.GuardarCambiosAsync(cts.Token);
            _consolidadorPostulacion.IntentarConsolidarAutomaticamenteAsync(_postulacionId, cts.Token);
        });
    }

    [Fact]
    public async Task Debe_NoInvocarConsolidador_Cuando_FallaValidacionOReglaDeNegocio()
    {
        var accionInvalida = () => _handler.HandleAsync(
            new AutorizarExcepcionEvaluadorUnicoCommand(Guid.Empty, JustificacionValida));
        await accionInvalida.Should().ThrowAsync<ValidationException>();

        var existente = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId, JustificacionValida, _ahora.AddHours(-1), _coordinadorId, Guid.NewGuid());
        _excepcionRepository
            .ObtenerPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(existente));

        var accionDuplicada = () => _handler.HandleAsync(
            new AutorizarExcepcionEvaluadorUnicoCommand(_postulacionId, JustificacionValida));
        await accionDuplicada.Should().ThrowAsync<ExcepcionEvaluadorUnicoYaAutorizadaException>();

        await _consolidadorPostulacion.DidNotReceiveWithAnyArgs()
            .IntentarConsolidarAutomaticamenteAsync(default, default);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
