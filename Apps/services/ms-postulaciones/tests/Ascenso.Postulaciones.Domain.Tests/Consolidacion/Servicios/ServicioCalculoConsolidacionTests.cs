using System.Reflection;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;

namespace Ascenso.Postulaciones.Domain.Tests.Consolidacion.Servicios;

public sealed class ServicioCalculoConsolidacionTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000000901");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000000902");
    private static readonly Guid _otraConvocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000000903");
    private static readonly Guid _jefaturaId = Guid.Parse("01968000-0000-7000-8000-000000000904");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000000905");
    private static readonly Guid _evaluador1Id = Guid.Parse("01968000-0000-7000-8000-000000000906");
    private static readonly Guid _evaluador2Id = Guid.Parse("01968000-0000-7000-8000-000000000907");
    private static readonly Guid _evaluador3Id = Guid.Parse("01968000-0000-7000-8000-000000000908");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000000909");
    private static readonly Guid _otraPostulacionId = Guid.Parse("01968000-0000-7000-8000-000000000910");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite = new(2026, 12, 10, 23, 59, 59, TimeSpan.Zero);

    [Fact]
    public void Debe_CalcularPromedioSinAlerta_Cuando_DosEvaluadoresTienenDiferenciaMenorAlUmbral()
    {
        var evaluaciones = new[]
        {
            CrearEvaluacionDefinitiva(_evaluador1Id, 80.00m),
            CrearEvaluacionDefinitiva(_evaluador2Id, 90.00m)
        };

        var resultado = ServicioCalculoConsolidacion.Calcular(
            _postulacionId,
            evaluaciones,
            excepcionEvaluadorUnico: null,
            ParametrosConsolidacion.PorDefecto);

        Assert.Equal(_convocatoriaId, resultado.ConvocatoriaId);
        Assert.Equal(_postulacionId, resultado.PostulacionId);
        Assert.False(resultado.ExcepcionEvaluadorUnico);
        Assert.Null(resultado.JustificacionEvaluadorUnico);
        Assert.Null(resultado.AutorizadorExcepcionId);
        Assert.Equal(2, resultado.CantidadEvaluacionesValidas);
        Assert.Equal(85.00m, resultado.PuntajeConsolidado);
        Assert.Equal(10.00m, resultado.DiferenciaEntreEvaluadores);
        Assert.Equal(20.00m, resultado.UmbralDiscrepanciaAplicado);
        Assert.False(resultado.AlertaDiscrepancia);
    }

    [Fact]
    public void Debe_RedondearConMidpointAwayFromZeroYCalcularExtremos_Cuando_HayTresEvaluadores()
    {
        var evaluaciones = new[]
        {
            CrearEvaluacionDefinitiva(_evaluador1Id, 80.00m),
            CrearEvaluacionDefinitiva(_evaluador2Id, 80.01m),
            CrearEvaluacionDefinitiva(_evaluador3Id, 80.01m)
        };

        var resultado = ServicioCalculoConsolidacion.Calcular(
            _postulacionId,
            evaluaciones,
            excepcionEvaluadorUnico: null,
            ParametrosConsolidacion.PorDefecto);

        Assert.Equal(3, resultado.CantidadEvaluacionesValidas);
        Assert.Equal(80.01m, resultado.PuntajeConsolidado);
        Assert.Equal(0.01m, resultado.DiferenciaEntreEvaluadores);
        Assert.False(resultado.AlertaDiscrepancia);
    }

    [Fact]
    public void Debe_AplicarRedondeoAwayFromZeroEnPuntoMedioExacto_Cuando_ElPromedioTerminaEnCincoMilesimas()
    {
        // (80.00 + 80.01) / 2 = 80.005 -> AwayFromZero = 80.01 (ToEven habría dado 80.00)
        var evaluaciones = new[]
        {
            CrearEvaluacionDefinitiva(_evaluador1Id, 80.00m),
            CrearEvaluacionDefinitiva(_evaluador2Id, 80.01m)
        };

        var resultado = ServicioCalculoConsolidacion.Calcular(
            _postulacionId,
            evaluaciones,
            excepcionEvaluadorUnico: null,
            ParametrosConsolidacion.PorDefecto);

        Assert.Equal(80.01m, resultado.PuntajeConsolidado);
    }

    [Fact]
    public void Debe_ActivarAlertaDiscrepancia_Cuando_DiferenciaSuperaEstrictamenteElUmbral()
    {
        var evaluaciones = new[]
        {
            CrearEvaluacionDefinitiva(_evaluador1Id, 60.00m),
            CrearEvaluacionDefinitiva(_evaluador2Id, 80.01m)
        };

        var resultado = ServicioCalculoConsolidacion.Calcular(
            _postulacionId,
            evaluaciones,
            excepcionEvaluadorUnico: null,
            ParametrosConsolidacion.PorDefecto);

        Assert.Equal(70.01m, resultado.PuntajeConsolidado);
        Assert.Equal(20.01m, resultado.DiferenciaEntreEvaluadores);
        Assert.Equal(20.00m, resultado.UmbralDiscrepanciaAplicado);
        Assert.True(resultado.AlertaDiscrepancia);
    }

    [Fact]
    public void Debe_ActivarAlertaPorExtremos_Cuando_HayTresEvaluadoresConDiscrepancia()
    {
        var evaluaciones = new[]
        {
            CrearEvaluacionDefinitiva(_evaluador1Id, 55.00m),
            CrearEvaluacionDefinitiva(_evaluador2Id, 72.00m),
            CrearEvaluacionDefinitiva(_evaluador3Id, 81.00m)
        };

        var resultado = ServicioCalculoConsolidacion.Calcular(
            _postulacionId,
            evaluaciones,
            excepcionEvaluadorUnico: null,
            ParametrosConsolidacion.PorDefecto);

        Assert.Equal(69.33m, resultado.PuntajeConsolidado);
        Assert.Equal(26.00m, resultado.DiferenciaEntreEvaluadores);
        Assert.True(resultado.AlertaDiscrepancia);
    }

    [Fact]
    public void Debe_NoActivarAlerta_Cuando_DiferenciaEsExactamenteIgualAlUmbral()
    {
        var evaluaciones = new[]
        {
            CrearEvaluacionDefinitiva(_evaluador1Id, 65.00m),
            CrearEvaluacionDefinitiva(_evaluador2Id, 85.00m)
        };

        var resultado = ServicioCalculoConsolidacion.Calcular(
            _postulacionId,
            evaluaciones,
            excepcionEvaluadorUnico: null,
            ParametrosConsolidacion.PorDefecto);

        Assert.Equal(75.00m, resultado.PuntajeConsolidado);
        Assert.Equal(20.00m, resultado.DiferenciaEntreEvaluadores);
        Assert.Equal(20.00m, resultado.UmbralDiscrepanciaAplicado);
        Assert.False(resultado.AlertaDiscrepancia);
    }

    [Fact]
    public void Debe_ConsolidarConExcepcionEvaluadorUnico_Cuando_ExisteUnaEvaluacionYAutorizacionValida()
    {
        var evaluaciones = new[]
        {
            CrearEvaluacionDefinitiva(_evaluador1Id, 88.50m)
        };
        var excepcion = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            "Solo existe un especialista disponible para evaluar el área específica.",
            _fechaBase.AddDays(3),
            _coordinadorId,
            Guid.CreateVersion7());

        var resultado = ServicioCalculoConsolidacion.Calcular(
            _postulacionId,
            evaluaciones,
            excepcion,
            ParametrosConsolidacion.PorDefecto);

        Assert.True(resultado.ExcepcionEvaluadorUnico);
        Assert.Equal(excepcion.Justificacion, resultado.JustificacionEvaluadorUnico);
        Assert.Equal(_coordinadorId, resultado.AutorizadorExcepcionId);
        Assert.Equal(1, resultado.CantidadEvaluacionesValidas);
        Assert.Equal(88.50m, resultado.PuntajeConsolidado);
        Assert.Equal(0.00m, resultado.DiferenciaEntreEvaluadores);
        Assert.Equal(20.00m, resultado.UmbralDiscrepanciaAplicado);
        Assert.False(resultado.AlertaDiscrepancia);
    }

    [Fact]
    public void Debe_PrevalecerModoMultiEvaluador_Cuando_ExistenDosOMasEvaluacionesAunConExcepcionPrevia()
    {
        var evaluaciones = new[]
        {
            CrearEvaluacionDefinitiva(_evaluador1Id, 70.00m),
            CrearEvaluacionDefinitiva(_evaluador2Id, 95.00m)
        };
        var excepcion = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            "Excepción previa antes de sumar un segundo evaluador.",
            _fechaBase.AddDays(3),
            _coordinadorId,
            Guid.CreateVersion7());

        var resultado = ServicioCalculoConsolidacion.Calcular(
            _postulacionId,
            evaluaciones,
            excepcion,
            ParametrosConsolidacion.PorDefecto);

        Assert.False(resultado.ExcepcionEvaluadorUnico);
        Assert.Null(resultado.JustificacionEvaluadorUnico);
        Assert.Null(resultado.AutorizadorExcepcionId);
        Assert.Equal(2, resultado.CantidadEvaluacionesValidas);
        Assert.Equal(82.50m, resultado.PuntajeConsolidado);
        Assert.Equal(25.00m, resultado.DiferenciaEntreEvaluadores);
        Assert.True(resultado.AlertaDiscrepancia);
    }

    [Fact]
    public void Debe_RechazarConEvaluacionesInsuficientes_Cuando_HayUnaEvaluacionSinExcepcion()
    {
        var evaluaciones = new[]
        {
            CrearEvaluacionDefinitiva(_evaluador1Id, 85.00m)
        };

        var excepcion = Assert.Throws<EvaluadoresInsuficientesException>(
            () => ServicioCalculoConsolidacion.Calcular(
                _postulacionId,
                evaluaciones,
                excepcionEvaluadorUnico: null,
                ParametrosConsolidacion.PorDefecto));

        Assert.Equal(EvaluadoresInsuficientesException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarConEvaluacionesInsuficientes_Cuando_NoHayEvaluacionesAunConExcepcion()
    {
        var excepcionAutorizada = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            "Autorización registrada antes de completar evaluaciones.",
            _fechaBase.AddDays(3),
            _coordinadorId,
            Guid.CreateVersion7());

        var excepcion = Assert.Throws<EvaluadoresInsuficientesException>(
            () => ServicioCalculoConsolidacion.Calcular(
                _postulacionId,
                Array.Empty<Evaluacion>(),
                excepcionAutorizada,
                ParametrosConsolidacion.PorDefecto));

        Assert.Equal(EvaluadoresInsuficientesException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarConConsolidacionInvalida_Cuando_HayBorradoresInvalidadasDuplicadosOIdsDiscordantes()
    {
        var definitiva1 = CrearEvaluacionDefinitiva(_evaluador1Id, 80.00m);
        var borrador = CrearEvaluacionBorrador(_evaluador2Id, 85.00m);
        var invalidada = CrearEvaluacionDefinitiva(_evaluador2Id, 85.00m);
        FijarEstadoEvaluacion(invalidada, EstadoEvaluacion.InvalidadaPorConflicto);

        var evaluadorDuplicado = CrearEvaluacionDefinitiva(_evaluador1Id, 90.00m);
        var otraConvocatoria = CrearEvaluacionDefinitiva(_evaluador2Id, 85.00m, convocatoriaId: _otraConvocatoriaId);
        var otraPostulacion = CrearEvaluacionDefinitiva(_evaluador2Id, 85.00m, postulacionId: _otraPostulacionId);
        var excepcionOtraPostulacion = AutorizacionExcepcionEvaluadorUnico.Crear(
            _otraPostulacionId,
            "Justificación correspondiente a otra postulación distinta.",
            _fechaBase.AddDays(3),
            _coordinadorId,
            Guid.CreateVersion7());

        Assert.Throws<ArgumentNullException>(() => ServicioCalculoConsolidacion.Calcular(
            _postulacionId, null!, null, ParametrosConsolidacion.PorDefecto));
        Assert.Throws<ArgumentNullException>(() => ServicioCalculoConsolidacion.Calcular(
            _postulacionId, [definitiva1], null, null!));

        Assert.Throws<ConsolidacionInvalidaException>(() => ServicioCalculoConsolidacion.Calcular(
            Guid.Empty, [definitiva1], null, ParametrosConsolidacion.PorDefecto));
        Assert.Throws<ConsolidacionInvalidaException>(() => ServicioCalculoConsolidacion.Calcular(
            _postulacionId, [definitiva1, borrador], null, ParametrosConsolidacion.PorDefecto));
        Assert.Throws<ConsolidacionInvalidaException>(() => ServicioCalculoConsolidacion.Calcular(
            _postulacionId, [definitiva1, invalidada], null, ParametrosConsolidacion.PorDefecto));
        Assert.Throws<ConsolidacionInvalidaException>(() => ServicioCalculoConsolidacion.Calcular(
            _postulacionId, [definitiva1, evaluadorDuplicado], null, ParametrosConsolidacion.PorDefecto));
        Assert.Throws<ConsolidacionInvalidaException>(() => ServicioCalculoConsolidacion.Calcular(
            _postulacionId, [definitiva1, otraConvocatoria], null, ParametrosConsolidacion.PorDefecto));
        Assert.Throws<ConsolidacionInvalidaException>(() => ServicioCalculoConsolidacion.Calcular(
            _postulacionId, [definitiva1, otraPostulacion], null, ParametrosConsolidacion.PorDefecto));
        Assert.Throws<ConsolidacionInvalidaException>(() => ServicioCalculoConsolidacion.Calcular(
            _postulacionId, [definitiva1], excepcionOtraPostulacion, ParametrosConsolidacion.PorDefecto));
        Assert.Throws<ConsolidacionInvalidaException>(() => ServicioCalculoConsolidacion.Calcular(
            _postulacionId, [definitiva1, null!], null, ParametrosConsolidacion.PorDefecto));
    }

    private static Evaluacion CrearEvaluacionDefinitiva(
        Guid evaluadorId,
        decimal puntaje,
        Guid? convocatoriaId = null,
        Guid? postulacionId = null)
    {
        var convId = convocatoriaId ?? _convocatoriaId;
        var postId = postulacionId ?? _postulacionId;
        var (rubrica, criterio) = CrearRubricaEscalaCien(convId);
        var asignacion = CrearAsignacion(convId, postId, evaluadorId);

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, puntaje, null)],
            null,
            _fechaBase.AddDays(5),
            evaluadorId);

        evaluacion.EnviarDefinitiva(
            rubrica,
            asignacion.FechaLimiteEvaluacion,
            _fechaBase.AddDays(6),
            evaluadorId);

        return evaluacion;
    }

    private static Evaluacion CrearEvaluacionBorrador(Guid evaluadorId, decimal puntaje)
    {
        var (rubrica, criterio) = CrearRubricaEscalaCien(_convocatoriaId);
        var asignacion = CrearAsignacion(_convocatoriaId, _postulacionId, evaluadorId);

        return Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, puntaje, null)],
            null,
            _fechaBase.AddDays(5),
            evaluadorId);
    }

    private static AsignacionEvaluacion CrearAsignacion(
        Guid convocatoriaId,
        Guid postulacionId,
        Guid evaluadorId)
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            convocatoriaId,
            "Comité Consolidación 2026-II",
            _jefaturaId,
            _fechaBase,
            _coordinadorId);

        var miembro = comite.AgregarMiembro(
            evaluadorId,
            TipoEvaluador.Externo,
            _fechaBase.AddDays(1),
            _coordinadorId);

        return AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            postulacionId,
            _fechaBase.AddDays(2),
            _fechaLimite,
            _coordinadorId);
    }

    private static (RubricaEvaluacion Rubrica, CriterioEvaluacion Criterio) CrearRubricaEscalaCien(
        Guid convocatoriaId)
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            convocatoriaId,
            "Rúbrica Escala 100",
            _fechaBase,
            _coordinadorId);

        var criterio = rubrica.AgregarCriterio(
            "Evaluación integral",
            "Puntaje global normalizado",
            100.00m,
            0.00m,
            100.00m,
            _fechaBase.AddMinutes(1),
            _coordinadorId);

        rubrica.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);
        return (rubrica, criterio);
    }

    private static void FijarEstadoEvaluacion(Evaluacion evaluacion, EstadoEvaluacion estado)
    {
        var propiedad = typeof(Evaluacion)
            .GetProperty(nameof(Evaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(propiedad);
        propiedad.SetValue(evaluacion, estado);
    }
}
