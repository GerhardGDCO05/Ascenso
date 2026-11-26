using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesComiteTests
{
    [Fact]
    public void Debe_ExponerCodigoEstadoYMensajePorDefecto_Cuando_SeCreanLasExcepcionesDeComite()
    {
        var casos = new (DomainException Excepcion, string CodigoEsperado, int EstadoHttpEsperado)[]
        {
            (new ComiteEvaluadorInvalidoException(), ComiteEvaluadorInvalidoException.CodigoError, 422),
            (new ComiteDuplicadoException(), ComiteDuplicadoException.CodigoError, 409),
            (new ComiteNoEncontradoException(), ComiteNoEncontradoException.CodigoError, 404),
            (new MiembroComiteDuplicadoException(), MiembroComiteDuplicadoException.CodigoError, 409),
            (new MiembroComiteNoEncontradoException(), MiembroComiteNoEncontradoException.CodigoError, 404),
            (new JefaturaNoDesvinculableException(), JefaturaNoDesvinculableException.CodigoError, 422)
        };

        Assert.Equal("COMITE_INVALIDO", ComiteEvaluadorInvalidoException.CodigoError);
        Assert.Equal("COMITE_DUPLICADO", ComiteDuplicadoException.CodigoError);
        Assert.Equal("COMITE_NO_ENCONTRADO", ComiteNoEncontradoException.CodigoError);
        Assert.Equal("MIEMBRO_COMITE_DUPLICADO", MiembroComiteDuplicadoException.CodigoError);
        Assert.Equal("MIEMBRO_COMITE_NO_ENCONTRADO", MiembroComiteNoEncontradoException.CodigoError);
        Assert.Equal("JEFATURA_NO_DESVINCULABLE", JefaturaNoDesvinculableException.CodigoError);

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
        const string mensajePersonalizado = "Detalle personalizado de la regla de negocio.";

        var excepciones = new ExcepcionReglaNegocio[]
        {
            new ComiteEvaluadorInvalidoException(mensajePersonalizado),
            new ComiteDuplicadoException(mensajePersonalizado),
            new ComiteNoEncontradoException(mensajePersonalizado),
            new MiembroComiteDuplicadoException(mensajePersonalizado),
            new MiembroComiteNoEncontradoException(mensajePersonalizado),
            new JefaturaNoDesvinculableException(mensajePersonalizado)
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
            new ComiteEvaluadorInvalidoException("   "),
            new ComiteDuplicadoException("   "),
            new ComiteNoEncontradoException("   "),
            new MiembroComiteDuplicadoException("   "),
            new MiembroComiteNoEncontradoException("   "),
            new JefaturaNoDesvinculableException("   ")
        };

        foreach (var excepcion in excepciones)
        {
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }
}
