using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesPostulacion;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.Consultas;

public sealed class ObtenerEvaluacionesPostulacionQueryHandlerTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968100-0000-7000-8000-000000000701");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968100-0000-7000-8000-000000000702");
    private static readonly Guid _coordinadorId = Guid.Parse("01968100-0000-7000-8000-000000000703");
    private static readonly Guid _postulacionId = Guid.Parse("01968100-0000-7000-8000-000000000704");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly IEvaluacionRepository _evaluacionRepository =
        Substitute.For<IEvaluacionRepository>();
    private readonly ObtenerEvaluacionesPostulacionQueryHandler _handler;

    public ObtenerEvaluacionesPostulacionQueryHandlerTests()
    {
        _handler = new ObtenerEvaluacionesPostulacionQueryHandler(
            _evaluacionRepository,
            new ObtenerEvaluacionesPostulacionQueryValidator());
    }

    [Fact]
    public async Task Debe_RetornarSoloEvaluacionesNoBorradorYPropagarToken_Cuando_CoordinadorConsultaPostulacion()
    {
        using var cts = new CancellationTokenSource();
        var (enviada, borrador) = CrearEvaluacionEnviadaYBorrador();

        _evaluacionRepository
            .ListarDefinitivasPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<IReadOnlyList<Evaluacion>>([enviada, borrador]));

        var resultado = await _handler.HandleAsync(
            new ObtenerEvaluacionesPostulacionQuery(_postulacionId),
            cts.Token);

        resultado.PostulacionId.Should().Be(_postulacionId);
        resultado.Evaluaciones.Should().ContainSingle();
        resultado.Evaluaciones[0].Id.Should().Be(enviada.Id);
        resultado.Evaluaciones[0].Estado.Should().Be("ENVIADA_DEFINITIVA");
        resultado.Evaluaciones[0].EvaluadorId.Should().Be(enviada.EvaluadorId);
        resultado.Evaluaciones[0].PuntajeTotalPonderado.Should().Be(80.00m);

        await _evaluacionRepository.Received(1)
            .ListarDefinitivasPorPostulacionAsync(_postulacionId, cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_PostulacionIdEsVacio()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerEvaluacionesPostulacionQuery(Guid.Empty),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();
        await _evaluacionRepository.DidNotReceiveWithAnyArgs()
            .ListarDefinitivasPorPostulacionAsync(default, default);
    }

    private static (Evaluacion Enviada, Evaluacion Borrador) CrearEvaluacionEnviadaYBorrador()
    {
        var eval1Id = Guid.NewGuid();
        var eval2Id = Guid.NewGuid();
        var comite = ComiteEvaluador.Crear(_programaBecaId, _convocatoriaId, "Comité", Guid.NewGuid(), _fechaBase, _coordinadorId);
        var m1 = comite.AgregarMiembro(eval1Id, TipoEvaluador.Externo, _fechaBase.AddDays(1), _coordinadorId);
        var m2 = comite.AgregarMiembro(eval2Id, TipoEvaluador.Interno, _fechaBase.AddDays(1), _coordinadorId);

        var a1 = AsignacionEvaluacion.Crear(comite, m1.Id, _postulacionId, _fechaBase.AddDays(2), _fechaBase.AddDays(15), _coordinadorId);
        var a2 = AsignacionEvaluacion.Crear(comite, m2.Id, _postulacionId, _fechaBase.AddDays(2), _fechaBase.AddDays(15), _coordinadorId);

        var rubrica = RubricaEvaluacion.Crear(_programaBecaId, _convocatoriaId, "Rúbrica", _fechaBase, _coordinadorId);
        var c1 = rubrica.AgregarCriterio("C1", "Desc", 100m, 0m, 20m, _fechaBase.AddMinutes(1), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);

        var enviada = Evaluacion.CrearBorrador(a1, rubrica, [new DatosCalificacionCriterio(c1.Id, 16m, null)], null, _fechaBase.AddDays(3), eval1Id);
        enviada.EnviarDefinitiva(rubrica, a1.FechaLimiteEvaluacion, _fechaBase.AddDays(4), eval1Id);

        var borrador = Evaluacion.CrearBorrador(a2, rubrica, [new DatosCalificacionCriterio(c1.Id, 12m, null)], null, _fechaBase.AddDays(3), eval2Id);

        return (enviada, borrador);
    }
}
