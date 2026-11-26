using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesConsolidacionTests
{
    [Fact]
    public void Debe_ExponerHerenciaCodigoEstadoYMensajePorDefecto_Cuando_SeCreanLasExcepcionesDeConsolidacion()
    {
        var noEncontradas = new (ExcepcionEntidadNoEncontrada Excepcion, string CodigoEsperado)[]
        {
            (new ResultadoConsolidadoNoEncontradoException(), ResultadoConsolidadoNoEncontradoException.CodigoError)
        };

        var reglasNegocio = new (ExcepcionReglaNegocio Excepcion, string CodigoEsperado, int EstadoHttpEsperado)[]
        {
            (new ResultadoConsolidadoDuplicadoException(), ResultadoConsolidadoDuplicadoException.CodigoError, 409),
            (new ConsolidacionInvalidaException(), ConsolidacionInvalidaException.CodigoError, 422),
            (new EvaluacionesPendientesParaConsolidarException(), EvaluacionesPendientesParaConsolidarException.CodigoError, 409),
            (new DiscrepanciaNoDetectadaException(), DiscrepanciaNoDetectadaException.CodigoError, 422),
            (new DiscrepanciaYaRevisadaException(), DiscrepanciaYaRevisadaException.CodigoError, 409),
            (new ComentarioRevisionDiscrepanciaInvalidoException(), ComentarioRevisionDiscrepanciaInvalidoException.CodigoError, 422),
            (new RankingYaConfirmadoException(), RankingYaConfirmadoException.CodigoError, 409)
        };

        Assert.Equal("RESULTADO_CONSOLIDADO_NO_ENCONTRADO", ResultadoConsolidadoNoEncontradoException.CodigoError);
        Assert.Equal("RESULTADO_CONSOLIDADO_DUPLICADO", ResultadoConsolidadoDuplicadoException.CodigoError);
        Assert.Equal("CONSOLIDACION_INVALIDA", ConsolidacionInvalidaException.CodigoError);
        Assert.Equal("EVALUACIONES_PENDIENTES", EvaluacionesPendientesParaConsolidarException.CodigoError);
        Assert.Equal("DISCREPANCIA_NO_DETECTADA", DiscrepanciaNoDetectadaException.CodigoError);
        Assert.Equal("DISCREPANCIA_YA_REVISADA", DiscrepanciaYaRevisadaException.CodigoError);
        Assert.Equal("COMENTARIO_REVISION_REQUERIDO", ComentarioRevisionDiscrepanciaInvalidoException.CodigoError);
        Assert.Equal("RANKING_YA_CONFIRMADO", RankingYaConfirmadoException.CodigoError);

        foreach (var (excepcion, codigoEsperado) in noEncontradas)
        {
            Assert.Equal(codigoEsperado, excepcion.Codigo);
            Assert.Equal(404, excepcion.EstadoHttp);
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }

        foreach (var (excepcion, codigoEsperado, estadoHttpEsperado) in reglasNegocio)
        {
            Assert.Equal(codigoEsperado, excepcion.Codigo);
            Assert.Equal(estadoHttpEsperado, excepcion.EstadoHttp);
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }

    [Fact]
    public void Debe_PreservarMensajePersonalizado_Cuando_SeEspecificaUnMensajeNoVacio()
    {
        const string mensajePersonalizado = "Detalle personalizado de la regla de consolidación.";

        var excepciones = new DomainException[]
        {
            new ResultadoConsolidadoNoEncontradoException(mensajePersonalizado),
            new ResultadoConsolidadoDuplicadoException(mensajePersonalizado),
            new ConsolidacionInvalidaException(mensajePersonalizado),
            new EvaluacionesPendientesParaConsolidarException(mensajePersonalizado),
            new DiscrepanciaNoDetectadaException(mensajePersonalizado),
            new DiscrepanciaYaRevisadaException(mensajePersonalizado),
            new ComentarioRevisionDiscrepanciaInvalidoException(mensajePersonalizado),
            new RankingYaConfirmadoException(mensajePersonalizado)
        };

        foreach (var excepcion in excepciones)
        {
            Assert.Equal(mensajePersonalizado, excepcion.Message);
        }
    }

    [Fact]
    public void Debe_UsarMensajePorDefecto_Cuando_ElMensajePersonalizadoEsSoloEspacios()
    {
        var excepciones = new DomainException[]
        {
            new ResultadoConsolidadoNoEncontradoException("   "),
            new ResultadoConsolidadoDuplicadoException("   "),
            new ConsolidacionInvalidaException("   "),
            new EvaluacionesPendientesParaConsolidarException("   "),
            new DiscrepanciaNoDetectadaException("   "),
            new DiscrepanciaYaRevisadaException("   "),
            new ComentarioRevisionDiscrepanciaInvalidoException("   "),
            new RankingYaConfirmadoException("   ")
        };

        foreach (var excepcion in excepciones)
        {
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }
}
