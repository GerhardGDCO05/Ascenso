using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConsolidarPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Servicios;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.Servicios;

public sealed class ConsolidadorPostulacionEvaluacionAdapterTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000002601");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000002602");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000002603");
    private static readonly Guid _jefaturaId = Guid.Parse("01968000-0000-7000-8000-000000002604");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000002605");
    private static readonly Guid _evaluador1Id = Guid.Parse("01968000-0000-7000-8000-000000002606");
    private static readonly Guid _evaluador2Id = Guid.Parse("01968000-0000-7000-8000-000000002607");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite = new(2026, 12, 10, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora = new(2026, 11, 26, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Debe_DelegarAConsolidarAutomaticamenteConUsuarioSistemaYPropagarToken_Cuando_SeInvocaElAdaptador()
    {
        using var cts = new CancellationTokenSource();
        var usuarioActual = Substitute.For<IUsuarioActual>();
        var resultadoRepo = Substitute.For<IResultadoConsolidadoRepository>();
        var consultaDatosRepo = Substitute.For<IConsultaDatosConsolidacionRepository>();
        var proveedorParametros = Substitute.For<IProveedorParametrosConsolidacion>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var (a1, e1) = CrearEvaluacionCompleta(_evaluador1Id, 80.00m);
        var (a2, e2) = CrearEvaluacionCompleta(_evaluador2Id, 90.00m);

        consultaDatosRepo
            .ListarAsignacionesVigentesPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<IReadOnlyList<AsignacionEvaluacion>>([a1, a2]));
        consultaDatosRepo
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<IReadOnlyList<Evaluacion>>([e1, e2]));
        consultaDatosRepo
            .ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<AutorizacionExcepcionEvaluadorUnico?>(null));
        proveedorParametros
            .ObtenerParametrosAsync(_convocatoriaId, e1.RubricaEvaluacionId, cts.Token)
            .Returns(Task.FromResult(ParametrosConsolidacion.PorDefecto));
        resultadoRepo
            .ObtenerParaActualizarPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(Task.FromResult<ResultadoConsolidadoRanking?>(null));

        ResultadoConsolidadoRanking? persistido = null;
        resultadoRepo
            .AgregarAsync(Arg.Do<ResultadoConsolidadoRanking>(r => persistido = r), cts.Token)
            .Returns(Task.CompletedTask);
        unitOfWork
            .GuardarCambiosAsync(cts.Token)
            .Returns(Task.FromResult(1));

        var handler = new ConsolidarPostulacionCommandHandler(
            usuarioActual,
            resultadoRepo,
            consultaDatosRepo,
            proveedorParametros,
            unitOfWork,
            new ConsolidarPostulacionCommandValidator(),
            new RelojFijo(_ahora));

        var adaptador = new ConsolidadorPostulacionEvaluacionAdapter(handler);

        await adaptador.IntentarConsolidarAutomaticamenteAsync(_postulacionId, cts.Token);

        persistido.Should().NotBeNull();
        persistido!.UsuarioCreacionId.Should().Be(UsuarioSistema.Id);
        persistido.PuntajeConsolidado.Should().Be(85.00m);
        await unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public void Debe_RechazarHandlerNulo_Cuando_SeConstruyeElAdaptador()
    {
        var accion = () => new ConsolidadorPostulacionEvaluacionAdapter(null!);

        accion.Should().Throw<ArgumentNullException>();
    }

    private static (AsignacionEvaluacion Asignacion, Evaluacion Evaluacion) CrearEvaluacionCompleta(
        Guid evaluadorId,
        decimal puntaje)
    {
        var (rubrica, criterio) = CrearRubricaEscalaCien();
        var comite = ComiteEvaluador.Crear(
            _programaBecaId, _convocatoriaId, "Comité Adaptador", _jefaturaId, _fechaBase, _coordinadorId);
        var miembro = comite.AgregarMiembro(evaluadorId, TipoEvaluador.Externo, _fechaBase.AddDays(1), _coordinadorId);
        var asignacion = AsignacionEvaluacion.Crear(
            comite, miembro.Id, _postulacionId, _fechaBase.AddDays(2), _fechaLimite, _coordinadorId);
        asignacion.IniciarEvaluacion(_fechaBase.AddDays(3), evaluadorId);

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, puntaje, null)],
            null,
            _fechaBase.AddDays(3),
            evaluadorId);
        evaluacion.EnviarDefinitiva(rubrica, asignacion.FechaLimiteEvaluacion, _fechaBase.AddDays(4), evaluadorId);
        asignacion.CompletarEvaluacion(_fechaBase.AddDays(4), evaluadorId);

        return (asignacion, evaluacion);
    }

    private static (RubricaEvaluacion Rubrica, CriterioEvaluacion Criterio) CrearRubricaEscalaCien()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId, _convocatoriaId, "Rúbrica Escala 100", _fechaBase, _coordinadorId);
        var criterio = rubrica.AgregarCriterio(
            "Global", "Puntaje global", 100.00m, 0.00m, 100.00m, _fechaBase.AddMinutes(1), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);
        return (rubrica, criterio);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
