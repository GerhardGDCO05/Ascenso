using System.Reflection;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Evaluaciones.Entidades;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;

namespace Ascenso.Postulaciones.Domain.Tests.Evaluaciones.Entidades;

public sealed class CalificacionCriterioTests
{
    private static readonly Guid _rubricaId = Guid.Parse("01968000-0000-7000-8000-000000000301");
    private static readonly Guid _evaluacionId = Guid.Parse("01968000-0000-7000-8000-000000000302");
    private static readonly Guid _creadorRubricaId = Guid.Parse("01968000-0000-7000-8000-000000000303");
    private static readonly Guid _evaluadorId = Guid.Parse("01968000-0000-7000-8000-000000000304");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 25, 10, 0, 0, TimeSpan.FromHours(-4));

    [Theory]
    [InlineData(1.00)]
    [InlineData(5.55)]
    [InlineData(10.00)]
    public void Debe_CrearCalificacionValida_Cuando_PuntajeEstaEnLosLimitesExactosOInteriorDelCriterio(
        decimal puntaje)
    {
        var criterio = CrearCriterio(1.00m, 10.00m);
        var datos = new DatosCalificacionCriterio(criterio.Id, puntaje, "  Observación técnica. ");

        var calificacion = CalificacionCriterio.Crear(
            _evaluacionId,
            criterio,
            datos,
            _fechaCreacion,
            _evaluadorId);

        Assert.IsAssignableFrom<EntidadAuditable>(calificacion);
        Assert.NotEqual(Guid.Empty, calificacion.Id);
        Assert.Equal(_evaluacionId, calificacion.EvaluacionId);
        Assert.Equal(criterio.Id, calificacion.CriterioEvaluacionId);
        Assert.Equal(puntaje, calificacion.PuntajeAsignado);
        Assert.Equal("Observación técnica.", calificacion.Comentario);
        Assert.Equal(_fechaCreacion.ToUniversalTime(), calificacion.FechaCreacion);
        Assert.Equal(_evaluadorId, calificacion.UsuarioCreacionId);
        Assert.Equal(1, calificacion.Version);
    }

    [Theory]
    [InlineData(0.99)]
    [InlineData(10.01)]
    [InlineData(-1.00)]
    public void Debe_RechazarCon422PuntajeFueraDeRango_Cuando_SeCreaFueraDelRangoDelCriterio(
        decimal puntajeInvalido)
    {
        var criterio = CrearCriterio(1.00m, 10.00m);
        var datos = new DatosCalificacionCriterio(criterio.Id, puntajeInvalido, null);

        var excepcion = Assert.Throws<PuntajeFueraDeRangoException>(() => CalificacionCriterio.Crear(
            _evaluacionId,
            criterio,
            datos,
            _fechaCreacion,
            _evaluadorId));

        Assert.Equal("PUNTAJE_FUERA_DE_RANGO", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_ActualizarPuntajeComentarioYAuditoria_Cuando_SeActualizaDentroDeRango()
    {
        var criterio = CrearCriterio(0.00m, 20.00m);
        var calificacion = CalificacionCriterio.Crear(
            _evaluacionId,
            criterio,
            new DatosCalificacionCriterio(criterio.Id, 12.00m, "Inicial"),
            _fechaCreacion,
            _evaluadorId);
        var fechaModificacion = _fechaCreacion.AddHours(2);

        calificacion.Actualizar(
            criterio,
            new DatosCalificacionCriterio(criterio.Id, 20.00m, "  Actualizado  "),
            fechaModificacion,
            _evaluadorId);

        Assert.Equal(20.00m, calificacion.PuntajeAsignado);
        Assert.Equal("Actualizado", calificacion.Comentario);
        Assert.Equal(2, calificacion.Version);
        Assert.Equal(fechaModificacion.ToUniversalTime(), calificacion.FechaUltimaModificacion);
        Assert.Equal(_evaluadorId, calificacion.UsuarioModificacionId);
    }

    [Fact]
    public void Debe_RechazarCon422PuntajeFueraDeRangoSinMutar_Cuando_SeActualizaFueraDeRango()
    {
        var criterio = CrearCriterio(0.00m, 20.00m);
        var calificacion = CalificacionCriterio.Crear(
            _evaluacionId,
            criterio,
            new DatosCalificacionCriterio(criterio.Id, 15.00m, "Original"),
            _fechaCreacion,
            _evaluadorId);

        var excepcion = Assert.Throws<PuntajeFueraDeRangoException>(() => calificacion.Actualizar(
            criterio,
            new DatosCalificacionCriterio(criterio.Id, 20.01m, "Nuevo"),
            _fechaCreacion.AddHours(1),
            _evaluadorId));

        Assert.Equal("PUNTAJE_FUERA_DE_RANGO", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal(15.00m, calificacion.PuntajeAsignado);
        Assert.Equal("Original", calificacion.Comentario);
        Assert.Equal(1, calificacion.Version);
    }

    [Fact]
    public void Debe_RechazarCriterioIncompatible_Cuando_DatosNoCoincidenConCriterio()
    {
        var criterio = CrearCriterio(0.00m, 20.00m);
        var datosOtroCriterio = new DatosCalificacionCriterio(Guid.NewGuid(), 10.00m, null);

        var excepcion = Assert.Throws<CriterioNoPerteneceARubricaException>(() => CalificacionCriterio.Crear(
            _evaluacionId,
            criterio,
            datosOtroCriterio,
            _fechaCreacion,
            _evaluadorId));

        Assert.Equal("CRITERIO_NO_PERTENECE_A_RUBRICA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_MantenerFabricaYActualizacionComoInternal_Cuando_SeInspeccionaEncapsulamiento()
    {
        var crear = typeof(CalificacionCriterio).GetMethod(
            nameof(CalificacionCriterio.Crear),
            BindingFlags.Static | BindingFlags.NonPublic);
        var actualizar = typeof(CalificacionCriterio).GetMethod(
            nameof(CalificacionCriterio.Actualizar),
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(crear);
        Assert.True(crear.IsAssembly);
        Assert.NotNull(actualizar);
        Assert.True(actualizar.IsAssembly);
    }

    private static CriterioEvaluacion CrearCriterio(decimal puntajeMinimo, decimal puntajeMaximo)
        => CriterioEvaluacion.Crear(
            _rubricaId,
            "Mérito académico",
            "Evaluación del historial académico",
            50.00m,
            puntajeMinimo,
            puntajeMaximo,
            1,
            _fechaCreacion.AddDays(-5),
            _creadorRubricaId);
}
