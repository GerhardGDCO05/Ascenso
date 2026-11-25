using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Evaluaciones.ObjetosValor;

public sealed class DatosCalificacionCriterioTests
{
    private static readonly Guid _criterioId = Guid.Parse("01968000-0000-7000-8000-000000000201");

    [Fact]
    public void Debe_CrearObjetoDeValorNormalizandoComentario_Cuando_LosDatosSonValidos()
    {
        var datos = new DatosCalificacionCriterio(_criterioId, 18.75m, "  Buen sustento técnico.  ");

        Assert.IsAssignableFrom<ValueObject>(datos);
        Assert.Equal(_criterioId, datos.CriterioEvaluacionId);
        Assert.Equal(18.75m, datos.PuntajeAsignado);
        Assert.Equal("Buen sustento técnico.", datos.Comentario);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \t\r\n  ")]
    public void Debe_NormalizarComentarioANulo_Cuando_EsNuloOEspacios(string? comentario)
    {
        var datos = new DatosCalificacionCriterio(_criterioId, 10.00m, comentario);

        Assert.Null(datos.Comentario);
    }

    [Fact]
    public void Debe_AceptarComentarioEnLongitudMaximaExacta_Cuando_Tiene1000Caracteres()
    {
        var comentario = new string('a', DatosCalificacionCriterio.LongitudMaximaComentario);

        var datos = new DatosCalificacionCriterio(_criterioId, 5.5m, $"  {comentario}  ");

        Assert.Equal(DatosCalificacionCriterio.LongitudMaximaComentario, datos.Comentario!.Length);
    }

    [Fact]
    public void Debe_RechazarCriterioVacio_Cuando_SeInstanciaDatosCalificacion()
    {
        var excepcion = Assert.Throws<EvaluacionInvalidaException>(
            () => new DatosCalificacionCriterio(Guid.Empty, 10m, "Comentario"));

        Assert.Equal("EVALUACION_INVALIDA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Theory]
    [InlineData(10.123)]
    [InlineData(0.001)]
    [InlineData(19.9999)]
    public void Debe_RechazarPuntajeConMasDeDosDecimales_Cuando_SeInstanciaDatosCalificacion(decimal puntaje)
    {
        var excepcion = Assert.Throws<EvaluacionInvalidaException>(
            () => new DatosCalificacionCriterio(_criterioId, puntaje, null));

        Assert.Equal("EVALUACION_INVALIDA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarComentarioQueExcede1000Caracteres_Cuando_SeInstanciaDatosCalificacion()
    {
        var comentarioExcedido = new string('x', DatosCalificacionCriterio.LongitudMaximaComentario + 1);

        var excepcion = Assert.Throws<EvaluacionInvalidaException>(
            () => new DatosCalificacionCriterio(_criterioId, 10m, comentarioExcedido));

        Assert.Equal("EVALUACION_INVALIDA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_AplicarIgualdadEstructural_Cuando_DosInstanciasTienenLosMismosValores()
    {
        var primero = new DatosCalificacionCriterio(_criterioId, 15.50m, "  Excelente  ");
        var segundo = new DatosCalificacionCriterio(_criterioId, 15.50m, "Excelente");

        Assert.Equal(primero, segundo);
        Assert.True(primero == segundo);
    }
}
