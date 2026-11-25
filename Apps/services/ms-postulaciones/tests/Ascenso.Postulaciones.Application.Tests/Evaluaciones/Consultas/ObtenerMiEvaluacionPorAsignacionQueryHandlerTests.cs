using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMiEvaluacionPorAsignacion;
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

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.Consultas;

public sealed class ObtenerMiEvaluacionPorAsignacionQueryHandlerTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968100-0000-7000-8000-000000000501");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968100-0000-7000-8000-000000000502");
    private static readonly Guid _jefaturaId = Guid.Parse("01968100-0000-7000-8000-000000000503");
    private static readonly Guid _coordinadorId = Guid.Parse("01968100-0000-7000-8000-000000000504");
    private static readonly Guid _evaluadorId = Guid.Parse("01968100-0000-7000-8000-000000000505");
    private static readonly Guid _otroEvaluadorId = Guid.Parse("01968100-0000-7000-8000-000000000506");
    private static readonly Guid _postulacionId = Guid.Parse("01968100-0000-7000-8000-000000000507");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IConsultaAsignacionParaEvaluacionRepository _asignacionRepository =
        Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
    private readonly IEvaluacionRepository _evaluacionRepository =
        Substitute.For<IEvaluacionRepository>();
    private readonly ObtenerMiEvaluacionPorAsignacionQueryHandler _handler;

    public ObtenerMiEvaluacionPorAsignacionQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_evaluadorId);

        _handler = new ObtenerMiEvaluacionPorAsignacionQueryHandler(
            _usuarioActual,
            _asignacionRepository,
            _evaluacionRepository,
            new ObtenerMiEvaluacionPorAsignacionQueryValidator());
    }

    [Fact]
    public async Task Debe_RetornarMiEvaluacionPropia_Cuando_ExisteYCorrespondeAlEvaluador()
    {
        using var cts = new CancellationTokenSource();
        var (asignacion, evaluacion) = CrearAsignacionYEvaluacion();
        _asignacionRepository.ObtenerPorIdAsync(asignacion.Id, cts.Token)
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacion));
        _evaluacionRepository.ObtenerPorAsignacionAsync(asignacion.Id, cts.Token)
            .Returns(Task.FromResult<Evaluacion?>(evaluacion));

        var resultado = await _handler.HandleAsync(
            new ObtenerMiEvaluacionPorAsignacionQuery(asignacion.Id),
            cts.Token);

        resultado.Id.Should().Be(evaluacion.Id);
        resultado.AsignacionEvaluacionId.Should().Be(asignacion.Id);
        resultado.Estado.Should().Be("BORRADOR");
        resultado.Calificaciones.Should().ContainSingle();
        await _asignacionRepository.Received(1).ObtenerPorIdAsync(asignacion.Id, cts.Token);
        await _evaluacionRepository.Received(1).ObtenerPorAsignacionAsync(asignacion.Id, cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarCon403ConfidencialSinConsultarEvaluacion_Cuando_AsignacionEsDeOtroEvaluador()
    {
        var (asignacion, _) = CrearAsignacionYEvaluacion();
        _usuarioActual.UsuarioId.Returns(_otroEvaluadorId);
        _asignacionRepository.ObtenerPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacion));

        var accion = () => _handler.HandleAsync(
            new ObtenerMiEvaluacionPorAsignacionQuery(asignacion.Id),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluacionConfidencialException>()
            .Where(ex => ex.Codigo == "EVALUACION_CONFIDENCIAL" && ex.EstadoHttp == 403);
        await _evaluacionRepository.DidNotReceiveWithAnyArgs()
            .ObtenerPorAsignacionAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_AsignacionOEvaluacionNoExisten()
    {
        var (asignacion, _) = CrearAsignacionYEvaluacion();

        var accionSinAsignacion = () => _handler.HandleAsync(
            new ObtenerMiEvaluacionPorAsignacionQuery(asignacion.Id),
            CancellationToken.None);
        await accionSinAsignacion.Should()
            .ThrowAsync<AsignacionEvaluacionNoEncontradaException>()
            .Where(ex => ex.Codigo == "ASIGNACION_NO_ENCONTRADA" && ex.EstadoHttp == 404);

        _asignacionRepository.ObtenerPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacion));
        var accionSinEvaluacion = () => _handler.HandleAsync(
            new ObtenerMiEvaluacionPorAsignacionQuery(asignacion.Id),
            CancellationToken.None);
        await accionSinEvaluacion.Should()
            .ThrowAsync<EvaluacionNoEncontradaException>()
            .Where(ex => ex.Codigo == "EVALUACION_NO_ENCONTRADA" && ex.EstadoHttp == 404);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_AsignacionIdEsVacio()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerMiEvaluacionPorAsignacionQuery(Guid.Empty),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();
    }

    private static (AsignacionEvaluacion Asignacion, Evaluacion Evaluacion) CrearAsignacionYEvaluacion()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia",
            _jefaturaId,
            _fechaBase,
            _coordinadorId);
        var miembro = comite.AgregarMiembro(_evaluadorId, TipoEvaluador.Externo, _fechaBase.AddDays(1), _coordinadorId);
        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaBase.AddDays(2),
            _fechaBase.AddDays(20),
            _coordinadorId);

        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Excelencia",
            _fechaBase,
            _coordinadorId);
        var c1 = rubrica.AgregarCriterio("C1", "Desc", 100.00m, 0.00m, 20.00m, _fechaBase.AddMinutes(1), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 17.00m, "Bien")],
            null,
            _fechaBase.AddDays(3),
            _evaluadorId);

        return (asignacion, evaluacion);
    }
}
