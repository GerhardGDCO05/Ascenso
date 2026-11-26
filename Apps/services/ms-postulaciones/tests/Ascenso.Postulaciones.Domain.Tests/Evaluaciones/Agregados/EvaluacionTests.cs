using System.Reflection;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;

namespace Ascenso.Postulaciones.Domain.Tests.Evaluaciones.Agregados;

public sealed class EvaluacionTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000000501");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000000502");
    private static readonly Guid _jefaturaId = Guid.Parse("01968000-0000-7000-8000-000000000503");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000000504");
    private static readonly Guid _evaluadorId = Guid.Parse("01968000-0000-7000-8000-000000000505");
    private static readonly Guid _otroEvaluadorId = Guid.Parse("01968000-0000-7000-8000-000000000506");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000000507");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite =
        new(2026, 12, 10, 23, 59, 59, TimeSpan.Zero);

    [Fact]
    public void Debe_CrearBorradorValidoConFechasUtcYComentariosNormalizados_Cuando_LosDatosSonValidos()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, _) = CrearRubricaPublicada();
        var ahora = new DateTimeOffset(2026, 11, 25, 14, 0, 0, TimeSpan.FromHours(-4));

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 16.50m, "  Buen promedio. ")],
            "  Evaluación inicial en curso.  ",
            ahora,
            _evaluadorId);

        Assert.IsAssignableFrom<AggregateRoot>(evaluacion);
        Assert.NotEqual(Guid.Empty, evaluacion.Id);
        Assert.Equal(asignacion.Id, evaluacion.AsignacionEvaluacionId);
        Assert.Equal(asignacion.PostulacionId, evaluacion.PostulacionId);
        Assert.Equal(asignacion.ConvocatoriaId, evaluacion.ConvocatoriaId);
        Assert.Equal(rubrica.Id, evaluacion.RubricaEvaluacionId);
        Assert.Equal(_evaluadorId, evaluacion.EvaluadorId);
        Assert.Equal(EstadoEvaluacion.Borrador, evaluacion.Estado);
        Assert.Equal(ahora.ToUniversalTime(), evaluacion.FechaInicio);
        Assert.Equal(ahora.ToUniversalTime(), evaluacion.FechaCreacion);
        Assert.Null(evaluacion.FechaEnvio);
        Assert.Null(evaluacion.PuntajeTotalPonderado);
        Assert.Equal("Evaluación inicial en curso.", evaluacion.ComentariosGenerales);
        Assert.Null(evaluacion.MotivoInvalidacion);
        Assert.Equal(1, evaluacion.Version);

        var calificacion = Assert.Single(evaluacion.Calificaciones);
        Assert.Equal(evaluacion.Id, calificacion.EvaluacionId);
        Assert.Equal(c1.Id, calificacion.CriterioEvaluacionId);
        Assert.Equal(16.50m, calificacion.PuntajeAsignado);
        Assert.Equal("Buen promedio.", calificacion.Comentario);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Debe_NormalizarComentariosGeneralesANulo_Cuando_SonVaciosOEspacios(string? comentarios)
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, _) = CrearRubricaPublicada();

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 10m, null)],
            comentarios,
            _fechaBase.AddDays(5),
            _evaluadorId);

        Assert.Null(evaluacion.ComentariosGenerales);
    }

    [Fact]
    public void Debe_RechazarComentariosGeneralesQueExceden2000Caracteres_Cuando_SeCreaBorrador()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, _) = CrearRubricaPublicada();
        var textoLargo = new string('a', Evaluacion.LongitudMaximaComentariosGenerales + 1);

        var excepcion = Assert.Throws<EvaluacionInvalidaException>(() => Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 10m, null)],
            textoLargo,
            _fechaBase.AddDays(5),
            _evaluadorId));

        Assert.Equal("EVALUACION_INVALIDA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_PermitirCrearBorradorEnElInstanteExactoDeFechaLimite_Cuando_PlazoEstaVigente()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, _) = CrearRubricaPublicada();

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 15m, null)],
            null,
            _fechaLimite,
            _evaluadorId);

        Assert.Equal(EstadoEvaluacion.Borrador, evaluacion.Estado);
    }

    [Fact]
    public void Debe_RechazarCon403Confidencial_Cuando_OtroEvaluadorIntentaCrearActualizarOEnviar()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, c2) = CrearRubricaPublicada();

        var exCrear = Assert.Throws<EvaluacionConfidencialException>(() => Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 10m, null)],
            null,
            _fechaBase.AddDays(5),
            _otroEvaluadorId));

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(c1.Id, 10m, null),
                new DatosCalificacionCriterio(c2.Id, 8m, null)
            ],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId);

        var exActualizar = Assert.Throws<EvaluacionConfidencialException>(() => evaluacion.ActualizarBorrador(
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 12m, null)],
            null,
            asignacion.FechaLimiteEvaluacion,
            _fechaBase.AddDays(6),
            _otroEvaluadorId));

        var exEnviar = Assert.Throws<EvaluacionConfidencialException>(() => evaluacion.EnviarDefinitiva(
            rubrica,
            asignacion.FechaLimiteEvaluacion,
            _fechaBase.AddDays(6),
            _otroEvaluadorId));

        Assert.Equal("EVALUACION_CONFIDENCIAL", exCrear.Codigo);
        Assert.Equal(403, exCrear.EstadoHttp);
        Assert.Equal("EVALUACION_CONFIDENCIAL", exActualizar.Codigo);
        Assert.Equal(403, exActualizar.EstadoHttp);
        Assert.Equal("EVALUACION_CONFIDENCIAL", exEnviar.Codigo);
        Assert.Equal(403, exEnviar.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCon409PlazoVencido_Cuando_AhoraSuperaFechaLimite()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, c2) = CrearRubricaPublicada();
        var despuesDelPlazo = _fechaLimite.AddSeconds(1);

        var exCrear = Assert.Throws<PlazoEvaluacionVencidoException>(() => Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 10m, null)],
            null,
            despuesDelPlazo,
            _evaluadorId));

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(c1.Id, 10m, null),
                new DatosCalificacionCriterio(c2.Id, 8m, null)
            ],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId);

        var exActualizar = Assert.Throws<PlazoEvaluacionVencidoException>(() => evaluacion.ActualizarBorrador(
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 12m, null)],
            null,
            asignacion.FechaLimiteEvaluacion,
            despuesDelPlazo,
            _evaluadorId));

        var exEnviar = Assert.Throws<PlazoEvaluacionVencidoException>(() => evaluacion.EnviarDefinitiva(
            rubrica,
            asignacion.FechaLimiteEvaluacion,
            despuesDelPlazo,
            _evaluadorId));

        Assert.Equal("PLAZO_EVALUACION_VENCIDO", exCrear.Codigo);
        Assert.Equal(409, exCrear.EstadoHttp);
        Assert.Equal("PLAZO_EVALUACION_VENCIDO", exActualizar.Codigo);
        Assert.Equal(409, exActualizar.EstadoHttp);
        Assert.Equal("PLAZO_EVALUACION_VENCIDO", exEnviar.Codigo);
        Assert.Equal(409, exEnviar.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCon409RubricaNoPublicada_Cuando_RubricaEstaEnBorrador()
    {
        var asignacion = CrearAsignacion();
        var rubricaBorrador = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica en borrador",
            _fechaBase,
            _coordinadorId);
        var c1 = rubricaBorrador.AgregarCriterio("C1", "Desc", 100m, 0m, 20m, _fechaBase.AddMinutes(1), _coordinadorId);

        var excepcion = Assert.Throws<RubricaNoPublicadaException>(() => Evaluacion.CrearBorrador(
            asignacion,
            rubricaBorrador,
            [new DatosCalificacionCriterio(c1.Id, 10m, null)],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId));

        Assert.Equal("RUBRICA_NO_PUBLICADA", excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCon422Invalida_Cuando_RubricaPerteneceAOtraConvocatoria()
    {
        var asignacion = CrearAsignacion();
        var rubricaOtraConvocatoria = RubricaEvaluacion.Crear(
            _programaBecaId,
            Guid.NewGuid(),
            "Rúbrica otra convocatoria",
            _fechaBase,
            _coordinadorId);
        var c1 = rubricaOtraConvocatoria.AgregarCriterio("C1", "Desc", 100m, 0m, 20m, _fechaBase.AddMinutes(1), _coordinadorId);
        rubricaOtraConvocatoria.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);

        var excepcion = Assert.Throws<EvaluacionInvalidaException>(() => Evaluacion.CrearBorrador(
            asignacion,
            rubricaOtraConvocatoria,
            [new DatosCalificacionCriterio(c1.Id, 10m, null)],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId));

        Assert.Equal("EVALUACION_INVALIDA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarAsignacionCompletadaOReasignada_Cuando_SeCreaBorrador()
    {
        var (rubrica, c1, _) = CrearRubricaPublicada();

        var asignacionCompletada = CrearAsignacion();
        asignacionCompletada.IniciarEvaluacion(_fechaBase.AddDays(3), _evaluadorId);
        asignacionCompletada.CompletarEvaluacion(_fechaBase.AddDays(4), _evaluadorId);

        var exCompletada = Assert.Throws<EvaluacionYaEnviadaException>(() => Evaluacion.CrearBorrador(
            asignacionCompletada,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 10m, null)],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId));
        Assert.Equal("EVALUACION_YA_ENVIADA", exCompletada.Codigo);

        var asignacionReasignada = CrearAsignacion();
        FijarEstadoAsignacion(asignacionReasignada, EstadoAsignacionEvaluacion.ReasignadaPorConflicto);

        var exReasignada = Assert.Throws<AsignacionNoEvaluableException>(() => Evaluacion.CrearBorrador(
            asignacionReasignada,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 10m, null)],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId));
        Assert.Equal("ASIGNACION_NO_EVALUABLE", exReasignada.Codigo);
    }

    [Fact]
    public void Debe_RechazarCalificacionesVaciasDuplicadasOAjenas_Cuando_SeCreaBorrador()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, _) = CrearRubricaPublicada();

        var exVacia = Assert.Throws<EvaluacionInvalidaException>(() => Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId));
        Assert.Equal("EVALUACION_INVALIDA", exVacia.Codigo);

        var exDuplicada = Assert.Throws<EvaluacionInvalidaException>(() => Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(c1.Id, 10m, null),
                new DatosCalificacionCriterio(c1.Id, 12m, null)
            ],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId));
        Assert.Equal("EVALUACION_INVALIDA", exDuplicada.Codigo);

        var exAjeno = Assert.Throws<CriterioNoPerteneceARubricaException>(() => Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(Guid.NewGuid(), 10m, null)],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId));
        Assert.Equal("CRITERIO_NO_PERTENECE_A_RUBRICA", exAjeno.Codigo);
    }

    [Fact]
    public void Debe_SincronizarCalificacionesPreservandoIdExistenteAgregandoYEliminando_Cuando_ActualizaBorrador()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, c2) = CrearRubricaPublicada();
        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 10m, "Inicial C1")],
            "Comentario 1",
            _fechaBase.AddDays(5),
            _evaluadorId);
        var idOriginalC1 = evaluacion.Calificaciones.Single().Id;

        evaluacion.ActualizarBorrador(
            rubrica,
            [
                new DatosCalificacionCriterio(c1.Id, 18m, "Actualizado C1"),
                new DatosCalificacionCriterio(c2.Id, 9m, "Nuevo C2")
            ],
            "Comentario 2",
            asignacion.FechaLimiteEvaluacion,
            _fechaBase.AddDays(6),
            _evaluadorId);

        Assert.Equal(2, evaluacion.Version);
        Assert.Equal("Comentario 2", evaluacion.ComentariosGenerales);
        Assert.Equal(2, evaluacion.Calificaciones.Count);

        var calC1 = evaluacion.Calificaciones.Single(c => c.CriterioEvaluacionId == c1.Id);
        Assert.Equal(idOriginalC1, calC1.Id);
        Assert.Equal(18m, calC1.PuntajeAsignado);
        Assert.Equal("Actualizado C1", calC1.Comentario);
        Assert.Equal(2, calC1.Version);

        var calC2 = evaluacion.Calificaciones.Single(c => c.CriterioEvaluacionId == c2.Id);
        Assert.Equal(9m, calC2.PuntajeAsignado);
        Assert.Equal("Nuevo C2", calC2.Comentario);
        Assert.Equal(1, calC2.Version);

        evaluacion.ActualizarBorrador(
            rubrica,
            [new DatosCalificacionCriterio(c2.Id, 10m, "Solo C2")],
            null,
            asignacion.FechaLimiteEvaluacion,
            _fechaBase.AddDays(7),
            _evaluadorId);

        Assert.Equal(3, evaluacion.Version);
        Assert.Null(evaluacion.ComentariosGenerales);
        var restante = Assert.Single(evaluacion.Calificaciones);
        Assert.Equal(c2.Id, restante.CriterioEvaluacionId);
        Assert.Equal(calC2.Id, restante.Id);
        Assert.Equal(10m, restante.PuntajeAsignado);
    }

    [Fact]
    public void Debe_EnviarDefinitivaConPuntajePonderadoYFechaEnvio_Cuando_TodosLosCriteriosEstanCalificados()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, c2) = CrearRubricaPublicada();
        // c1 [0, 20] peso 60, puntaje 15 => 75 * 0.60 = 45
        // c2 [0, 10] peso 40, puntaje 8 => 80 * 0.40 = 32
        // total = 77.00
        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(c1.Id, 15.00m, "C1"),
                new DatosCalificacionCriterio(c2.Id, 8.00m, "C2")
            ],
            "Conclusión final",
            _fechaBase.AddDays(5),
            _evaluadorId);
        var fechaEnvio = new DateTimeOffset(2026, 11, 28, 16, 45, 0, TimeSpan.FromHours(-4));

        evaluacion.EnviarDefinitiva(
            rubrica,
            asignacion.FechaLimiteEvaluacion,
            fechaEnvio,
            _evaluadorId);

        Assert.Equal(EstadoEvaluacion.EnviadaDefinitiva, evaluacion.Estado);
        Assert.Equal(77.00m, evaluacion.PuntajeTotalPonderado);
        Assert.Equal(fechaEnvio.ToUniversalTime(), evaluacion.FechaEnvio);
        Assert.Equal(fechaEnvio.ToUniversalTime(), evaluacion.FechaUltimaModificacion);
        Assert.Equal(2, evaluacion.Version);
    }

    [Fact]
    public void Debe_RechazarEnvioCon422Incompleta_Cuando_FaltanCriteriosPorCalificar()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, _) = CrearRubricaPublicada();
        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 15.00m, null)],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId);

        var excepcion = Assert.Throws<EvaluacionIncompletaException>(() => evaluacion.EnviarDefinitiva(
            rubrica,
            asignacion.FechaLimiteEvaluacion,
            _fechaBase.AddDays(6),
            _evaluadorId));

        Assert.Equal("EVALUACION_INCOMPLETA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal(EstadoEvaluacion.Borrador, evaluacion.Estado);
        Assert.Null(evaluacion.FechaEnvio);
        Assert.Null(evaluacion.PuntajeTotalPonderado);
        Assert.Equal(1, evaluacion.Version);
    }

    [Fact]
    public void Debe_GarantizarInmutabilidadTotal_Cuando_EvaluacionYaFueEnviadaDefinitiva()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, c2) = CrearRubricaPublicada();
        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(c1.Id, 20.00m, null),
                new DatosCalificacionCriterio(c2.Id, 10.00m, null)
            ],
            "Final",
            _fechaBase.AddDays(5),
            _evaluadorId);
        evaluacion.EnviarDefinitiva(rubrica, asignacion.FechaLimiteEvaluacion, _fechaBase.AddDays(6), _evaluadorId);

        var exActualizar = Assert.Throws<EvaluacionYaEnviadaException>(() => evaluacion.ActualizarBorrador(
            rubrica,
            [
                new DatosCalificacionCriterio(c1.Id, 5.00m, null),
                new DatosCalificacionCriterio(c2.Id, 5.00m, null)
            ],
            "Intento",
            asignacion.FechaLimiteEvaluacion,
            _fechaBase.AddDays(7),
            _evaluadorId));

        var exReenviar = Assert.Throws<EvaluacionYaEnviadaException>(() => evaluacion.EnviarDefinitiva(
            rubrica,
            asignacion.FechaLimiteEvaluacion,
            _fechaBase.AddDays(7),
            _evaluadorId));

        Assert.Equal("EVALUACION_YA_ENVIADA", exActualizar.Codigo);
        Assert.Equal(409, exActualizar.EstadoHttp);
        Assert.Equal("EVALUACION_YA_ENVIADA", exReenviar.Codigo);
        Assert.Equal(409, exReenviar.EstadoHttp);
        Assert.Equal(100.00m, evaluacion.PuntajeTotalPonderado);
        Assert.Equal(2, evaluacion.Version);
    }

    [Fact]
    public void Debe_RechazarCon409Invalidada_Cuando_EvaluacionEstaInvalidadaPorConflicto()
    {
        var asignacion = CrearAsignacion();
        var (rubrica, c1, c2) = CrearRubricaPublicada();
        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(c1.Id, 10m, null),
                new DatosCalificacionCriterio(c2.Id, 5m, null)
            ],
            null,
            _fechaBase.AddDays(5),
            _evaluadorId);
        FijarEstadoEvaluacion(evaluacion, EstadoEvaluacion.InvalidadaPorConflicto);

        var exActualizar = Assert.Throws<EvaluacionInvalidadaException>(() => evaluacion.ActualizarBorrador(
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 12m, null)],
            null,
            asignacion.FechaLimiteEvaluacion,
            _fechaBase.AddDays(6),
            _evaluadorId));

        var exEnviar = Assert.Throws<EvaluacionInvalidadaException>(() => evaluacion.EnviarDefinitiva(
            rubrica,
            asignacion.FechaLimiteEvaluacion,
            _fechaBase.AddDays(6),
            _evaluadorId));

        Assert.Equal("EVALUACION_INVALIDADA", exActualizar.Codigo);
        Assert.Equal(409, exActualizar.EstadoHttp);
        Assert.Equal("EVALUACION_INVALIDADA", exEnviar.Codigo);
        Assert.Equal(409, exEnviar.EstadoHttp);
    }

    private static AsignacionEvaluacion CrearAsignacion()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia 2026-II",
            _jefaturaId,
            _fechaBase,
            _coordinadorId);

        var miembro = comite.AgregarMiembro(
            _evaluadorId,
            TipoEvaluador.Externo,
            _fechaBase.AddDays(1),
            _coordinadorId);

        return AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaBase.AddDays(2),
            _fechaLimite,
            _coordinadorId);
    }

    private static (RubricaEvaluacion Rubrica, CriterioEvaluacion C1, CriterioEvaluacion C2)
        CrearRubricaPublicada()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Excelencia 2026-II",
            _fechaBase,
            _coordinadorId);

        var c1 = rubrica.AgregarCriterio("Mérito académico", "Promedio", 60.00m, 0.00m, 20.00m, _fechaBase.AddMinutes(1), _coordinadorId);
        var c2 = rubrica.AgregarCriterio("Impacto social", "Trayectoria", 40.00m, 0.00m, 10.00m, _fechaBase.AddMinutes(2), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(3), _coordinadorId);

        return (rubrica, c1, c2);
    }

    private static void FijarEstadoAsignacion(AsignacionEvaluacion asignacion, EstadoAsignacionEvaluacion estado)
    {
        var propiedad = typeof(AsignacionEvaluacion)
            .GetProperty(nameof(AsignacionEvaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(propiedad);
        propiedad.SetValue(asignacion, estado);
    }

    private static void FijarEstadoEvaluacion(Evaluacion evaluacion, EstadoEvaluacion estado)
    {
        var propiedad = typeof(Evaluacion)
            .GetProperty(nameof(Evaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(propiedad);
        propiedad.SetValue(evaluacion, estado);
    }
}
