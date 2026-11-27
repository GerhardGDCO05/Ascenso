using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesListaEsperaTests
{
    [Fact]
    public void Debe_ExponerHerenciaDeEntidadNoEncontradaCodigoYEstado404_Cuando_SeCreanLasExcepcionesNoEncontradasDeListaEspera()
    {
        var excepciones = new (ExcepcionEntidadNoEncontrada Excepcion, string CodigoEsperado)[]
        {
            (new EntradaListaEsperaNoEncontradaException(), EntradaListaEsperaNoEncontradaException.CodigoError),
            (new ListaEsperaSinCandidatosException(), ListaEsperaSinCandidatosException.CodigoError)
        };

        Assert.Equal("ENTRADA_LISTA_ESPERA_NO_ENCONTRADA", EntradaListaEsperaNoEncontradaException.CodigoError);
        Assert.Equal("LISTA_ESPERA_SIN_CANDIDATOS", ListaEsperaSinCandidatosException.CodigoError);

        foreach (var (excepcion, codigoEsperado) in excepciones)
        {
            Assert.IsAssignableFrom<ExcepcionEntidadNoEncontrada>(excepcion);
            Assert.IsAssignableFrom<DomainException>(excepcion);
            Assert.Equal(codigoEsperado, excepcion.Codigo);
            Assert.Equal(404, excepcion.EstadoHttp);
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }

    [Fact]
    public void Debe_ExponerHerenciaDeReglaNegocioCodigoEstadoYMensajePorDefecto_Cuando_SeCreanLasExcepcionesDeReglaDeListaEspera()
    {
        var excepciones = new (ExcepcionReglaNegocio Excepcion, string CodigoEsperado, int EstadoHttpEsperado)[]
        {
            (new EntradaListaEsperaYaPromovidaException(), EntradaListaEsperaYaPromovidaException.CodigoError, 409),
            (new EntradaListaEsperaYaElegibleException(), EntradaListaEsperaYaElegibleException.CodigoError, 409),
            (new EntradaListaEsperaDuplicadaException(), EntradaListaEsperaDuplicadaException.CodigoError, 409),
            (new OrdenListaEsperaVioladoException(), OrdenListaEsperaVioladoException.CodigoError, 409),
            (new SinCupoDisponiblePromocionException(), SinCupoDisponiblePromocionException.CodigoError, 409),
            (new PresupuestoInsuficientePromocionException(), PresupuestoInsuficientePromocionException.CodigoError, 422),
            (new ListaEsperaInvalidaException(), ListaEsperaInvalidaException.CodigoError, 422),
            (new MotivoPromocionInvalidoException(), MotivoPromocionInvalidoException.CodigoError, 422)
        };

        Assert.Equal("ENTRADA_LISTA_ESPERA_YA_PROMOVIDA", EntradaListaEsperaYaPromovidaException.CodigoError);
        Assert.Equal("ENTRADA_LISTA_ESPERA_YA_ELEGIBLE", EntradaListaEsperaYaElegibleException.CodigoError);
        Assert.Equal("ENTRADA_LISTA_ESPERA_DUPLICADA", EntradaListaEsperaDuplicadaException.CodigoError);
        Assert.Equal("ORDEN_LISTA_ESPERA_VIOLADO", OrdenListaEsperaVioladoException.CodigoError);
        Assert.Equal("SIN_CUPO_DISPONIBLE_PROMOCION", SinCupoDisponiblePromocionException.CodigoError);
        Assert.Equal("PRESUPUESTO_INSUFICIENTE_PROMOCION", PresupuestoInsuficientePromocionException.CodigoError);
        Assert.Equal("LISTA_ESPERA_INVALIDA", ListaEsperaInvalidaException.CodigoError);
        Assert.Equal("MOTIVO_PROMOCION_INVALIDO", MotivoPromocionInvalidoException.CodigoError);

        foreach (var (excepcion, codigoEsperado, estadoHttpEsperado) in excepciones)
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
        const string mensajePersonalizado = "Detalle personalizado de la excepción de lista de espera.";

        var excepciones = new DomainException[]
        {
            new EntradaListaEsperaNoEncontradaException(mensajePersonalizado),
            new ListaEsperaSinCandidatosException(mensajePersonalizado),
            new EntradaListaEsperaYaPromovidaException(mensajePersonalizado),
            new EntradaListaEsperaYaElegibleException(mensajePersonalizado),
            new EntradaListaEsperaDuplicadaException(mensajePersonalizado),
            new OrdenListaEsperaVioladoException(mensajePersonalizado),
            new SinCupoDisponiblePromocionException(mensajePersonalizado),
            new PresupuestoInsuficientePromocionException(mensajePersonalizado),
            new ListaEsperaInvalidaException(mensajePersonalizado),
            new MotivoPromocionInvalidoException(mensajePersonalizado)
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
            new EntradaListaEsperaNoEncontradaException("   "),
            new ListaEsperaSinCandidatosException("   "),
            new EntradaListaEsperaYaPromovidaException("   "),
            new EntradaListaEsperaYaElegibleException("   "),
            new EntradaListaEsperaDuplicadaException("   "),
            new OrdenListaEsperaVioladoException("   "),
            new SinCupoDisponiblePromocionException("   "),
            new PresupuestoInsuficientePromocionException("   "),
            new ListaEsperaInvalidaException("   "),
            new MotivoPromocionInvalidoException("   ")
        };

        foreach (var excepcion in excepciones)
        {
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }
}
