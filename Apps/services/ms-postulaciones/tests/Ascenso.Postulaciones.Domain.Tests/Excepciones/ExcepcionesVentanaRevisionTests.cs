using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesVentanaRevisionTests
{
    [Fact]
    public void Debe_ExponerHerenciaCodigoEstadoYMensajePorDefecto_Cuando_SeCreanLasExcepcionesDeVentanaRevision()
    {
        var convocatoriaNoEncontrada = new ConvocatoriaNoEncontradaException();

        Assert.IsAssignableFrom<ExcepcionEntidadNoEncontrada>(convocatoriaNoEncontrada);
        Assert.Equal("CONVOCATORIA_NO_ENCONTRADA", ConvocatoriaNoEncontradaException.CodigoError);
        Assert.Equal(ConvocatoriaNoEncontradaException.CodigoError, convocatoriaNoEncontrada.Codigo);
        Assert.Equal(404, convocatoriaNoEncontrada.EstadoHttp);
        Assert.False(string.IsNullOrWhiteSpace(convocatoriaNoEncontrada.Message));

        var reglasNegocio = new (ExcepcionReglaNegocio Excepcion, string CodigoEsperado, int EstadoHttpEsperado)[]
        {
            (new PeriodoRevisionNoIniciadoException(), PeriodoRevisionNoIniciadoException.CodigoError, 409),
            (new PeriodoRevisionCerradoException(), PeriodoRevisionCerradoException.CodigoError, 409),
            (new PeriodoRevisionAbiertoException(), PeriodoRevisionAbiertoException.CodigoError, 409),
            (new ConvocatoriaCanceladaException(), ConvocatoriaCanceladaException.CodigoError, 409)
        };

        Assert.Equal("PERIODO_REVISION_NO_INICIADO", PeriodoRevisionNoIniciadoException.CodigoError);
        Assert.Equal("PERIODO_REVISION_CERRADO", PeriodoRevisionCerradoException.CodigoError);
        Assert.Equal("PERIODO_REVISION_ABIERTO", PeriodoRevisionAbiertoException.CodigoError);
        Assert.Equal("CONVOCATORIA_CANCELADA", ConvocatoriaCanceladaException.CodigoError);

        foreach (var (excepcion, codigoEsperado, estadoHttpEsperado) in reglasNegocio)
        {
            Assert.IsAssignableFrom<ExcepcionReglaNegocio>(excepcion);
            Assert.IsAssignableFrom<DomainException>(excepcion);
            Assert.Equal(codigoEsperado, excepcion.Codigo);
            Assert.Equal(estadoHttpEsperado, excepcion.EstadoHttp);
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }

    [Fact]
    public void Debe_PreservarMensajePersonalizado_Cuando_SeEspecificaUnMensajeNoVacio()
    {
        const string mensajePersonalizado = "Detalle personalizado de la ventana de revisión.";

        var excepciones = new DomainException[]
        {
            new ConvocatoriaNoEncontradaException(mensajePersonalizado),
            new PeriodoRevisionNoIniciadoException(mensajePersonalizado),
            new PeriodoRevisionCerradoException(mensajePersonalizado),
            new PeriodoRevisionAbiertoException(mensajePersonalizado),
            new ConvocatoriaCanceladaException(mensajePersonalizado)
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
            new ConvocatoriaNoEncontradaException("   "),
            new PeriodoRevisionNoIniciadoException("   "),
            new PeriodoRevisionCerradoException("   "),
            new PeriodoRevisionAbiertoException("   "),
            new ConvocatoriaCanceladaException("   ")
        };

        foreach (var excepcion in excepciones)
        {
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }
}
