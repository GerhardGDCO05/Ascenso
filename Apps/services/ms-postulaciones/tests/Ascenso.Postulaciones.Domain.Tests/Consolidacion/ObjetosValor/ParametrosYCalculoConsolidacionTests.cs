using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Consolidacion.ObjetosValor;

public sealed class ParametrosYCalculoConsolidacionTests
{
    [Fact]
    public void Debe_ExponerValoresPorDefectoYHerenciaValueObject_Cuando_SeCreaParametrosConsolidacion()
    {
        var parametros = ParametrosConsolidacion.PorDefecto;

        Assert.IsAssignableFrom<ValueObject>(parametros);
        Assert.Equal(2, ParametrosConsolidacion.EvaluadoresMinimosPorDefecto);
        Assert.Equal(20.00m, ParametrosConsolidacion.UmbralDiscrepanciaPorDefecto);
        Assert.Equal(0.00m, ParametrosConsolidacion.UmbralMinimoExclusivo);
        Assert.Equal(100.00m, ParametrosConsolidacion.UmbralMaximo);
        Assert.Equal(2, parametros.EvaluadoresMinimosRequeridos);
        Assert.Equal(20.00m, parametros.UmbralDiscrepanciaPuntos);
    }

    [Theory]
    [InlineData(2, 0.01)]
    [InlineData(2, 20.00)]
    [InlineData(3, 15.75)]
    [InlineData(5, 100.00)]
    public void Debe_CrearParametrosConsolidacion_Cuando_LosValoresEstanEnRangoValido(
        int evaluadoresMinimos,
        decimal umbralDiscrepancia)
    {
        var parametros = new ParametrosConsolidacion(evaluadoresMinimos, umbralDiscrepancia);

        Assert.Equal(evaluadoresMinimos, parametros.EvaluadoresMinimosRequeridos);
        Assert.Equal(umbralDiscrepancia, parametros.UmbralDiscrepanciaPuntos);
        Assert.Equal(new ParametrosConsolidacion(evaluadoresMinimos, umbralDiscrepancia), parametros);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Debe_RechazarParametrosConsolidacion_Cuando_EvaluadoresMinimosEsMenorADos(
        int evaluadoresInvalidos)
    {
        var excepcion = Assert.Throws<ConsolidacionInvalidaException>(
            () => new ParametrosConsolidacion(evaluadoresInvalidos, 20.00m));

        Assert.Equal(ConsolidacionInvalidaException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Theory]
    [InlineData(-5.00)]
    [InlineData(0.00)]
    [InlineData(100.01)]
    [InlineData(15.125)]
    [InlineData(20.001)]
    public void Debe_RechazarParametrosConsolidacion_Cuando_UmbralEstaFueraDeRangoOTieneMasDeDosDecimales(
        decimal umbralInvalido)
    {
        var excepcion = Assert.Throws<ConsolidacionInvalidaException>(
            () => new ParametrosConsolidacion(2, umbralInvalido));

        Assert.Equal(ConsolidacionInvalidaException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_CrearResultadoCalculoMultiEvaluador_Cuando_LasInvariantesSonValidas()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();

        var resultado = new ResultadoCalculoConsolidacion(
            convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 85.00m,
            diferenciaEntreEvaluadores: 25.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: true);

        Assert.IsAssignableFrom<ValueObject>(resultado);
        Assert.Equal(convocatoriaId, resultado.ConvocatoriaId);
        Assert.Equal(postulacionId, resultado.PostulacionId);
        Assert.False(resultado.ExcepcionEvaluadorUnico);
        Assert.Null(resultado.JustificacionEvaluadorUnico);
        Assert.Null(resultado.AutorizadorExcepcionId);
        Assert.Equal(2, resultado.CantidadEvaluacionesValidas);
        Assert.Equal(85.00m, resultado.PuntajeConsolidado);
        Assert.Equal(25.00m, resultado.DiferenciaEntreEvaluadores);
        Assert.Equal(20.00m, resultado.UmbralDiscrepanciaAplicado);
        Assert.True(resultado.AlertaDiscrepancia);
    }

    [Fact]
    public void Debe_CrearResultadoCalculoConExcepcionEvaluadorUnico_Cuando_LasInvariantesSonValidas()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var autorizadorId = Guid.CreateVersion7();

        var resultado = new ResultadoCalculoConsolidacion(
            convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: true,
            justificacionEvaluadorUnico: "  Un único especialista disponible para el área.  ",
            autorizadorExcepcionId: autorizadorId,
            cantidadEvaluacionesValidas: 1,
            puntajeConsolidado: 88.50m,
            diferenciaEntreEvaluadores: 0.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        Assert.True(resultado.ExcepcionEvaluadorUnico);
        Assert.Equal("Un único especialista disponible para el área.", resultado.JustificacionEvaluadorUnico);
        Assert.Equal(autorizadorId, resultado.AutorizadorExcepcionId);
        Assert.Equal(1, resultado.CantidadEvaluacionesValidas);
        Assert.Equal(88.50m, resultado.PuntajeConsolidado);
        Assert.Equal(0.00m, resultado.DiferenciaEntreEvaluadores);
        Assert.False(resultado.AlertaDiscrepancia);
    }

    [Fact]
    public void Debe_RechazarResultadoCalculo_Cuando_SeIncumplenInvariantesDeIdsRangosDecimalesExcepcionOAlerta()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var autorizadorId = Guid.CreateVersion7();

        // Convocatoria vacía
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            Guid.Empty, postulacionId, false, null, null, 2, 80.00m, 10.00m, 20.00m, false));

        // Postulación vacía
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, Guid.Empty, false, null, null, 2, 80.00m, 10.00m, 20.00m, false));

        // Puntaje fuera de rango o >2 decimales
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, -0.01m, 10.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, 100.01m, 10.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, 80.125m, 10.00m, 20.00m, false));

        // Diferencia fuera de rango o >2 decimales
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, 80.00m, -1.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, 80.00m, 100.01m, 20.00m, true));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, 80.00m, 10.125m, 20.00m, false));

        // Umbral inválido
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, 80.00m, 10.00m, 0.00m, true));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, 80.00m, 10.00m, 100.01m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, 80.00m, 10.00m, 20.005m, false));

        // Incoherencias en modo multi-evaluador
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 1, 80.00m, 0.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, "No debería existir", null, 2, 80.00m, 10.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, autorizadorId, 2, 80.00m, 10.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, 80.00m, 25.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, false, null, null, 2, 80.00m, 20.00m, 20.00m, true));

        // Incoherencias en modo excepción evaluador único
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, true, "Justificación válida", autorizadorId, 2, 80.00m, 0.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, true, "   ", autorizadorId, 1, 80.00m, 0.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, true, "Justificación válida", null, 1, 80.00m, 0.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, true, "Justificación válida", Guid.Empty, 1, 80.00m, 0.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, true, "Justificación válida", autorizadorId, 1, 80.00m, 5.00m, 20.00m, false));
        Assert.Throws<ConsolidacionInvalidaException>(() => new ResultadoCalculoConsolidacion(
            convocatoriaId, postulacionId, true, "Justificación válida", autorizadorId, 1, 80.00m, 0.00m, 20.00m, true));
    }
}
