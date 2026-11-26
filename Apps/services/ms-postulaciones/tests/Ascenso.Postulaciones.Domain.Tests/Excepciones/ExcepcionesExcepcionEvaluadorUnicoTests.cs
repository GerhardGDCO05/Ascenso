using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesExcepcionEvaluadorUnicoTests
{
    [Fact]
    public void Debe_ExponerCodigoEstadoYMensajePorDefecto_Cuando_SeCreanLasExcepcionesDeExcepcionEvaluadorUnico()
    {
        var casos = new (DomainException Excepcion, string CodigoEsperado, int EstadoHttpEsperado)[]
        {
            (new JustificacionExcepcionInvalidaException(), JustificacionExcepcionInvalidaException.CodigoError, 400),
            (new ExcepcionEvaluadorUnicoYaAutorizadaException(), ExcepcionEvaluadorUnicoYaAutorizadaException.CodigoError, 409),
            (new ExcepcionEvaluadorUnicoNoAplicableException(), ExcepcionEvaluadorUnicoNoAplicableException.CodigoError, 422),
            (new ExcepcionEvaluadorUnicoNoEncontradaException(), ExcepcionEvaluadorUnicoNoEncontradaException.CodigoError, 404)
        };

        Assert.Equal("JUSTIFICACION_REQUERIDA", JustificacionExcepcionInvalidaException.CodigoError);
        Assert.Equal("EXCEPCION_YA_AUTORIZADA", ExcepcionEvaluadorUnicoYaAutorizadaException.CodigoError);
        Assert.Equal("EXCEPCION_NO_APLICABLE", ExcepcionEvaluadorUnicoNoAplicableException.CodigoError);
        Assert.Equal("EXCEPCION_NO_ENCONTRADA", ExcepcionEvaluadorUnicoNoEncontradaException.CodigoError);

        foreach (var (excepcion, codigoEsperado, estadoHttpEsperado) in casos)
        {
            Assert.IsAssignableFrom<ExcepcionReglaNegocio>(excepcion);
            Assert.Equal(codigoEsperado, excepcion.Codigo);
            Assert.Equal(estadoHttpEsperado, excepcion.EstadoHttp);
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }

    [Fact]
    public void Debe_PreservarMensajePersonalizado_Cuando_SeEspecificaUnMensajeNoVacio()
    {
        const string mensajePersonalizado = "Detalle personalizado de la excepción de evaluador único.";

        var excepciones = new ExcepcionReglaNegocio[]
        {
            new JustificacionExcepcionInvalidaException(mensajePersonalizado),
            new ExcepcionEvaluadorUnicoYaAutorizadaException(mensajePersonalizado),
            new ExcepcionEvaluadorUnicoNoAplicableException(mensajePersonalizado),
            new ExcepcionEvaluadorUnicoNoEncontradaException(mensajePersonalizado)
        };

        foreach (var excepcion in excepciones)
        {
            Assert.Equal(mensajePersonalizado, excepcion.Message);
        }
    }

    [Fact]
    public void Debe_UsarMensajePorDefecto_Cuando_ElMensajePersonalizadoEsSoloEspacios()
    {
        var excepciones = new ExcepcionReglaNegocio[]
        {
            new JustificacionExcepcionInvalidaException("   "),
            new ExcepcionEvaluadorUnicoYaAutorizadaException("   "),
            new ExcepcionEvaluadorUnicoNoAplicableException("   "),
            new ExcepcionEvaluadorUnicoNoEncontradaException("   ")
        };

        foreach (var excepcion in excepciones)
        {
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }
}
