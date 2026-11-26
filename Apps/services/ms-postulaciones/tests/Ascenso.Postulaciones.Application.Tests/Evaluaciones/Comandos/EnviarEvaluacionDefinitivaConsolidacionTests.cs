using Ascenso.Postulaciones.Application.Consolidacion.Interfaces;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.EnviarEvaluacionDefinitiva;
using Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.Comandos;

public sealed class EnviarEvaluacionDefinitivaConsolidacionTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968300-0000-7000-8000-000000000401");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968300-0000-7000-8000-000000000402");
    private static readonly Guid _jefaturaId = Guid.Parse("01968300-0000-7000-8000-000000000403");
    private static readonly Guid _coordinadorId = Guid.Parse("01968300-0000-7000-8000-000000000404");
    private static readonly Guid _evaluadorId = Guid.Parse("01968300-0000-7000-8000-000000000405");
    private static readonly Guid _postulacionId = Guid.Parse("01968300-0000-7000-8000-000000000406");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite = new(2026, 12, 10, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora = new(2026, 11, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IConsultaAsignacionParaEvaluacionRepository _asignacionRepository =
        Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient =
        Substitute.For<IConvocatoriaEvaluacionClient>();
    private readonly IEvaluacionRepository _evaluacionRepository =
        Substitute.For<IEvaluacionRepository>();
    private readonly IComiteEvaluadorRepository _comiteRepository =
        Substitute.For<IComiteEvaluadorRepository>();
    private readonly IConsultaRubricaParaEvaluacionRepository _rubricaRepository =
        Substitute.For<IConsultaRubricaParaEvaluacionRepository>();
    private readonly IBloqueadorRubricaEvaluacion _bloqueadorRubrica =
        Substitute.For<IBloqueadorRubricaEvaluacion>();
    private readonly IConsolidadorPostulacionEvaluacion _consolidadorPostulacion =
        Substitute.For<IConsolidadorPostulacionEvaluacion>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public EnviarEvaluacionDefinitivaConsolidacionTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_evaluadorId);
        _unitOfWork.GuardarCambiosAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(
                    call.ArgAt<Guid>(0),
                    EstadoConvocatoriaExterna.Publicada,
                    _ahora.AddDays(-10),
                    _fechaLimite.AddDays(10))));
    }

    [Fact]
    public async Task Debe_InvocarConsolidadorDespuesDeGuardarCambios_Cuando_EnvioDefinitivoEsExitoso()
    {
        using var cts = new CancellationTokenSource();
        var (comite, asignacion, rubrica, evaluacion) = CrearEscenario(completo: true);
        ConfigurarRepositorios(comite, asignacion, rubrica, evaluacion, cts.Token);
        var handler = CrearHandler();

        await handler.HandleAsync(new EnviarEvaluacionDefinitivaCommand(asignacion.Id), cts.Token);

        Received.InOrder(() =>
        {
            _unitOfWork.GuardarCambiosAsync(cts.Token);
            _consolidadorPostulacion.IntentarConsolidarAutomaticamenteAsync(_postulacionId, cts.Token);
        });
    }

    [Fact]
    public async Task Debe_NoInvocarConsolidador_Cuando_FallaValidacionOReglaDeNegocio()
    {
        var handler = CrearHandler();

        var accionInvalida = () => handler.HandleAsync(new EnviarEvaluacionDefinitivaCommand(Guid.Empty));
        await accionInvalida.Should().ThrowAsync<ValidationException>();

        var (comite, asignacion, rubrica, evaluacionIncompleta) = CrearEscenario(completo: false);
        ConfigurarRepositorios(comite, asignacion, rubrica, evaluacionIncompleta, CancellationToken.None);

        var accionIncompleta = () => handler.HandleAsync(new EnviarEvaluacionDefinitivaCommand(asignacion.Id));
        await accionIncompleta.Should().ThrowAsync<EvaluacionIncompletaException>();

        await _consolidadorPostulacion.DidNotReceiveWithAnyArgs()
            .IntentarConsolidarAutomaticamenteAsync(default, default);
    }

    private EnviarEvaluacionDefinitivaCommandHandler CrearHandler()
        => new(
            _usuarioActual,
            _asignacionRepository,
            _convocatoriaClient,
            _evaluacionRepository,
            _comiteRepository,
            _rubricaRepository,
            _bloqueadorRubrica,
            _consolidadorPostulacion,
            _unitOfWork,
            new EnviarEvaluacionDefinitivaCommandValidator(),
            new RelojFijo(_ahora));

    private void ConfigurarRepositorios(
        ComiteEvaluador comite,
        AsignacionEvaluacion asignacion,
        RubricaEvaluacion rubrica,
        Evaluacion evaluacion,
        CancellationToken ct)
    {
        _asignacionRepository
            .ObtenerParaActualizarPorIdAsync(asignacion.Id, ct)
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacion));
        _evaluacionRepository
            .ObtenerParaActualizarPorAsignacionAsync(asignacion.Id, ct)
            .Returns(Task.FromResult<Evaluacion?>(evaluacion));
        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, ct)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));
        _rubricaRepository
            .ObtenerPorProgramaYConvocatoriaAsync(comite.ProgramaBecaId, comite.ConvocatoriaId, ct)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));
    }

    private static (ComiteEvaluador Comite, AsignacionEvaluacion Asignacion, RubricaEvaluacion Rubrica, Evaluacion Evaluacion)
        CrearEscenario(bool completo)
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId, _convocatoriaId, "Comité Envío", _jefaturaId, _fechaBase, _coordinadorId);
        var miembro = comite.AgregarMiembro(_evaluadorId, TipoEvaluador.Externo, _fechaBase.AddDays(1), _coordinadorId);
        var asignacion = AsignacionEvaluacion.Crear(
            comite, miembro.Id, _postulacionId, _fechaBase.AddDays(2), _fechaLimite, _coordinadorId);
        asignacion.IniciarEvaluacion(_fechaBase.AddDays(3), _evaluadorId);

        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId, _convocatoriaId, "Rúbrica Envío", _fechaBase, _coordinadorId);
        var c1 = rubrica.AgregarCriterio("C1", "Desc 1", 60.00m, 0.00m, 100.00m, _fechaBase.AddMinutes(1), _coordinadorId);
        var c2 = rubrica.AgregarCriterio("C2", "Desc 2", 40.00m, 0.00m, 100.00m, _fechaBase.AddMinutes(2), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(3), _coordinadorId);
        rubrica.Bloquear(MotivoBloqueoRubrica.PrimeraCalificacion, _fechaBase.AddMinutes(4), _coordinadorId);

        var calificaciones = completo
            ? new[]
            {
                new DatosCalificacionCriterio(c1.Id, 80.00m, null),
                new DatosCalificacionCriterio(c2.Id, 70.00m, null)
            }
            : new[]
            {
                new DatosCalificacionCriterio(c1.Id, 80.00m, null)
            };

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            calificaciones,
            null,
            _fechaBase.AddDays(3),
            _evaluadorId);

        return (comite, asignacion, rubrica, evaluacion);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
