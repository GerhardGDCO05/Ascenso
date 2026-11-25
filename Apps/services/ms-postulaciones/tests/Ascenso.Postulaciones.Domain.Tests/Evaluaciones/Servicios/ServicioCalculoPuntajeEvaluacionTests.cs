using System.Reflection;
using Ascenso.Postulaciones.Domain.Evaluaciones.Entidades;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Evaluaciones.Servicios;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;

namespace Ascenso.Postulaciones.Domain.Tests.Evaluaciones.Servicios;

public sealed class ServicioCalculoPuntajeEvaluacionTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000000401");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000000402");
    private static readonly Guid _creadorId = Guid.Parse("01968000-0000-7000-8000-000000000403");
    private static readonly Guid _evaluadorId = Guid.Parse("01968000-0000-7000-8000-000000000404");
    private static readonly Guid _evaluacionId = Guid.Parse("01968000-0000-7000-8000-000000000405");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_CalcularCeroExacto_Cuando_TodosLosCriteriosTienenPuntajeMinimo()
    {
        var (rubrica, c1, c2, c3) = CrearRubricaTresCriterios();
        var calificaciones = new[]
        {
            CrearCalificacion(c1, 0.00m),
            CrearCalificacion(c2, 1.00m),
            CrearCalificacion(c3, 0.00m)
        };

        var total = ServicioCalculoPuntajeEvaluacion.CalcularPuntajeTotalPonderado(rubrica, calificaciones);

        Assert.Equal(0.00m, total);
    }

    [Fact]
    public void Debe_CalcularCienExacto_Cuando_TodosLosCriteriosTienenPuntajeMaximo()
    {
        var (rubrica, c1, c2, c3) = CrearRubricaTresCriterios();
        var calificaciones = new[]
        {
            CrearCalificacion(c1, 10.00m),
            CrearCalificacion(c2, 5.00m),
            CrearCalificacion(c3, 20.00m)
        };

        var total = ServicioCalculoPuntajeEvaluacion.CalcularPuntajeTotalPonderado(rubrica, calificaciones);

        Assert.Equal(100.00m, total);
    }

    [Fact]
    public void Debe_CalcularPuntajePonderadoExacto_Cuando_HayPesosFraccionariosYRangosDistintos()
    {
        var (rubrica, c1, c2, c3) = CrearRubricaTresCriterios();
        // c1 [0, 10], peso 33.33, puntaje 8 => norm 80 => 26.664
        // c2 [1, 5], peso 33.33, puntaje 3 => norm 50 => 16.665
        // c3 [0, 20], peso 33.34, puntaje 15 => norm 75 => 25.005
        // suma = 68.334 => 68.33
        var calificaciones = new[]
        {
            CrearCalificacion(c1, 8.00m),
            CrearCalificacion(c2, 3.00m),
            CrearCalificacion(c3, 15.00m)
        };

        var total = ServicioCalculoPuntajeEvaluacion.CalcularPuntajeTotalPonderado(rubrica, calificaciones);

        Assert.Equal(68.33m, total);
    }

    [Fact]
    public void Debe_RedondearConMidpointAwayFromZero_Cuando_ElResultadoCaeEnPuntoMedioExacto()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Redondeo",
            _fechaBase,
            _creadorId);
        var c1 = rubrica.AgregarCriterio("C1", "Desc 1", 50.00m, 0.00m, 100.00m, _fechaBase.AddMinutes(1), _creadorId);
        var c2 = rubrica.AgregarCriterio("C2", "Desc 2", 50.00m, 0.00m, 100.00m, _fechaBase.AddMinutes(2), _creadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(3), _creadorId);

        // 1.01 * 0.50 + 0 * 0.50 = 0.505 -> AwayFromZero = 0.51 (ToEven daría 0.50)
        var calificaciones = new[]
        {
            CrearCalificacion(c1, 1.01m),
            CrearCalificacion(c2, 0.00m)
        };

        var total = ServicioCalculoPuntajeEvaluacion.CalcularPuntajeTotalPonderado(rubrica, calificaciones);

        Assert.Equal(0.51m, total);
    }

    [Fact]
    public void Debe_RechazarCon422Incompleta_Cuando_FaltanCriteriosPorCalificar()
    {
        var (rubrica, c1, _, _) = CrearRubricaTresCriterios();
        var calificaciones = new[] { CrearCalificacion(c1, 7.50m) };

        var excepcion = Assert.Throws<EvaluacionIncompletaException>(
            () => ServicioCalculoPuntajeEvaluacion.CalcularPuntajeTotalPonderado(rubrica, calificaciones));

        Assert.Equal("EVALUACION_INCOMPLETA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Contains("2", excepcion.Message);
    }

    [Fact]
    public void Debe_RechazarCon422CriterioNoPerteneceARubrica_Cuando_HayCriterioAjeno()
    {
        var (rubrica, c1, c2, c3) = CrearRubricaTresCriterios();
        var criterioAjeno = CriterioEvaluacion.Crear(
            Guid.NewGuid(),
            "Ajeno",
            "Otro criterio",
            100.00m,
            0.00m,
            10.00m,
            1,
            _fechaBase,
            _creadorId);
        var calificaciones = new[]
        {
            CrearCalificacion(c1, 5.00m),
            CrearCalificacion(c2, 3.00m),
            CrearCalificacion(c3, 10.00m),
            CrearCalificacion(criterioAjeno, 5.00m)
        };

        var excepcion = Assert.Throws<CriterioNoPerteneceARubricaException>(
            () => ServicioCalculoPuntajeEvaluacion.CalcularPuntajeTotalPonderado(rubrica, calificaciones));

        Assert.Equal("CRITERIO_NO_PERTENECE_A_RUBRICA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCon422Invalida_Cuando_HayCriteriosDuplicados()
    {
        var (rubrica, c1, c2, c3) = CrearRubricaTresCriterios();
        var calificaciones = new[]
        {
            CrearCalificacion(c1, 5.00m),
            CrearCalificacion(c1, 6.00m),
            CrearCalificacion(c2, 3.00m),
            CrearCalificacion(c3, 10.00m)
        };

        var excepcion = Assert.Throws<EvaluacionInvalidaException>(
            () => ServicioCalculoPuntajeEvaluacion.CalcularPuntajeTotalPonderado(rubrica, calificaciones));

        Assert.Equal("EVALUACION_INVALIDA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCon422PuntajeFueraDeRango_Cuando_CalificacionTienePuntajeFueraDelRangoDelCriterio()
    {
        var (rubrica, c1, c2, c3) = CrearRubricaTresCriterios();
        var calificacionAlterada = CrearCalificacion(c2, 3.00m);
        var propiedadPuntaje = typeof(CalificacionCriterio)
            .GetProperty(nameof(CalificacionCriterio.PuntajeAsignado), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(propiedadPuntaje);
        propiedadPuntaje.SetValue(calificacionAlterada, 5.50m);

        var calificaciones = new[]
        {
            CrearCalificacion(c1, 5.00m),
            calificacionAlterada,
            CrearCalificacion(c3, 10.00m)
        };

        var excepcion = Assert.Throws<PuntajeFueraDeRangoException>(
            () => ServicioCalculoPuntajeEvaluacion.CalcularPuntajeTotalPonderado(rubrica, calificaciones));

        Assert.Equal("PUNTAJE_FUERA_DE_RANGO", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    private static (RubricaEvaluacion Rubrica, CriterioEvaluacion C1, CriterioEvaluacion C2, CriterioEvaluacion C3)
        CrearRubricaTresCriterios()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Excelencia 2026-II",
            _fechaBase,
            _creadorId);

        var c1 = rubrica.AgregarCriterio("Académico", "Desc 1", 33.33m, 0.00m, 10.00m, _fechaBase.AddMinutes(1), _creadorId);
        var c2 = rubrica.AgregarCriterio("Investigación", "Desc 2", 33.33m, 1.00m, 5.00m, _fechaBase.AddMinutes(2), _creadorId);
        var c3 = rubrica.AgregarCriterio("Impacto", "Desc 3", 33.34m, 0.00m, 20.00m, _fechaBase.AddMinutes(3), _creadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(4), _creadorId);

        return (rubrica, c1, c2, c3);
    }

    private static CalificacionCriterio CrearCalificacion(CriterioEvaluacion criterio, decimal puntaje)
        => CalificacionCriterio.Crear(
            _evaluacionId,
            criterio,
            new DatosCalificacionCriterio(criterio.Id, puntaje, null),
            _fechaBase.AddDays(1),
            _evaluadorId);
}
