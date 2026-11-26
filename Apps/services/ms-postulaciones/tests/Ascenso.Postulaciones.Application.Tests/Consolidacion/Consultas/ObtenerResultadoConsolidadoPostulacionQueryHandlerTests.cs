using System.Reflection;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerResultadoConsolidadoPostulacion;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.Consultas;

public sealed class ObtenerResultadoConsolidadoPostulacionQueryHandlerTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000002401");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000002402");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000002403");
    private static readonly Guid _jefaturaId = Guid.Parse("01968000-0000-7000-8000-000000002404");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000002405");
    private static readonly Guid _evaluador1Id = Guid.Parse("01968000-0000-7000-8000-000000002406");
    private static readonly Guid _evaluador2Id = Guid.Parse("01968000-0000-7000-8000-000000002407");
    private static readonly Guid _evaluador3Id = Guid.Parse("01968000-0000-7000-8000-000000002408");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite = new(2026, 12, 10, 23, 59, 59, TimeSpan.Zero);

    private readonly IResultadoConsolidadoRepository _resultadoRepository = Substitute.For<IResultadoConsolidadoRepository>();
    private readonly IConsultaDatosConsolidacionRepository _consultaDatosRepository = Substitute.For<IConsultaDatosConsolidacionRepository>();
    private readonly ObtenerResultadoConsolidadoPostulacionQueryHandler _handler;

    public ObtenerResultadoConsolidadoPostulacionQueryHandlerTests()
    {
        _handler = new ObtenerResultadoConsolidadoPostulacionQueryHandler(
            _resultadoRepository,
            _consultaDatosRepository,
            new ObtenerResultadoConsolidadoPostulacionQueryValidator());
    }

    [Fact]
    public async Task Debe_RetornarDetalleConNotasIndividualesOrdenadasExcluyendoNoDefinitivas_Cuando_ExisteResultado()
    {
        using var cts = new CancellationTokenSource();
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId, _postulacionId, false, null, null, 2, 85.50m, 21.00m, 20.00m, true);
        var agregado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase.AddDays(6), UsuarioSistema.Id);

        var eval2 = CrearEvaluacionDefinitiva(_evaluador2Id, 96.00m, _fechaBase.AddDays(5));
        var eval1 = CrearEvaluacionDefinitiva(_evaluador1Id, 75.00m, _fechaBase.AddDays(4));
        var borrador = CrearEvaluacionBorrador(_evaluador3Id, 40.00m);
        var invalidada = CrearEvaluacionDefinitiva(_evaluador3Id, 30.00m, _fechaBase.AddDays(3));
        FijarEstadoEvaluacion(invalidada, EstadoEvaluacion.InvalidadaPorConflicto);

        _resultadoRepository
            .ObtenerPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(agregado));
        _consultaDatosRepository
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<IReadOnlyList<Evaluacion>>([eval2, borrador, invalidada, eval1]));

        var detalle = await _handler.HandleAsync(
            new ObtenerResultadoConsolidadoPostulacionQuery(_postulacionId),
            cts.Token);

        detalle.Id.Should().Be(agregado.Id);
        detalle.PostulacionId.Should().Be(_postulacionId);
        detalle.EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
        detalle.PuntajeConsolidado.Should().Be(85.50m);
        detalle.Evaluaciones.Should().HaveCount(2);
        detalle.Evaluaciones[0].EvaluacionId.Should().Be(eval1.Id);
        detalle.Evaluaciones[1].EvaluacionId.Should().Be(eval2.Id);

        await _resultadoRepository.Received(1).ObtenerPorPostulacionAsync(_postulacionId, cts.Token);
        await _consultaDatosRepository.Received(1)
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(_postulacionId, cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_NoExisteResultadoConsolidado()
    {
        _resultadoRepository
            .ObtenerPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(null));

        var accion = () => _handler.HandleAsync(new ObtenerResultadoConsolidadoPostulacionQuery(_postulacionId));

        await accion.Should()
            .ThrowAsync<ResultadoConsolidadoNoEncontradoException>()
            .Where(ex => ex.Codigo == "RESULTADO_CONSOLIDADO_NO_ENCONTRADO" && ex.EstadoHttp == 404);

        await _consultaDatosRepository.DidNotReceiveWithAnyArgs()
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(default, default);
    }

    [Fact]
    public async Task Debe_FallarValidacion_Cuando_PostulacionIdEsVacio()
    {
        var accion = () => _handler.HandleAsync(new ObtenerResultadoConsolidadoPostulacionQuery(Guid.Empty));

        await accion.Should().ThrowAsync<ValidationException>();
        await _resultadoRepository.DidNotReceiveWithAnyArgs().ObtenerPorPostulacionAsync(default, default);
    }

    private static Evaluacion CrearEvaluacionDefinitiva(Guid evaluadorId, decimal puntaje, DateTimeOffset fechaEnvio)
    {
        var (rubrica, criterio) = CrearRubrica();
        var asignacion = CrearAsignacion(evaluadorId);
        var eval = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, puntaje, null)],
            null,
            _fechaBase.AddDays(2),
            evaluadorId);
        eval.EnviarDefinitiva(rubrica, asignacion.FechaLimiteEvaluacion, fechaEnvio, evaluadorId);
        return eval;
    }

    private static Evaluacion CrearEvaluacionBorrador(Guid evaluadorId, decimal puntaje)
    {
        var (rubrica, criterio) = CrearRubrica();
        var asignacion = CrearAsignacion(evaluadorId);
        return Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, puntaje, null)],
            null,
            _fechaBase.AddDays(2),
            evaluadorId);
    }

    private static AsignacionEvaluacion CrearAsignacion(Guid evaluadorId)
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId, _convocatoriaId, "Comité Consulta", _jefaturaId, _fechaBase, _coordinadorId);
        var miembro = comite.AgregarMiembro(evaluadorId, TipoEvaluador.Externo, _fechaBase.AddHours(1), _coordinadorId);
        return AsignacionEvaluacion.Crear(
            comite, miembro.Id, _postulacionId, _fechaBase.AddHours(2), _fechaLimite, _coordinadorId);
    }

    private static (RubricaEvaluacion Rubrica, CriterioEvaluacion Criterio) CrearRubrica()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId, _convocatoriaId, "Rúbrica Consulta", _fechaBase, _coordinadorId);
        var criterio = rubrica.AgregarCriterio(
            "Global", "Global", 100.00m, 0.00m, 100.00m, _fechaBase.AddMinutes(1), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);
        return (rubrica, criterio);
    }

    private static void FijarEstadoEvaluacion(Evaluacion evaluacion, EstadoEvaluacion estado)
    {
        var propiedad = typeof(Evaluacion)
            .GetProperty(nameof(Evaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);
        propiedad!.SetValue(evaluacion, estado);
    }
}
