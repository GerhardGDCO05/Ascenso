using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesCasoLimiteTests
{
    [Fact]
    public void Debe_ExponerHerenciaCodigoEstadoYMensajePorDefecto_Cuando_SeCreanLasExcepcionesDeCasosLimite()
    {
        var excepciones = new (ExcepcionReglaNegocio Excepcion, string CodigoEsperado, int EstadoHttpEsperado)[]
        {
            (new CasoLimiteNoDetectadoException(), CasoLimiteNoDetectadoException.CodigoError, 422),
            (new CasoLimiteYaResueltoException(), CasoLimiteYaResueltoException.CodigoError, 409),
            (new SeleccionCasoLimiteInvalidaException(), SeleccionCasoLimiteInvalidaException.CodigoError, 422),
            (new SeleccionCasoLimiteExcedeDisponibilidadException(), SeleccionCasoLimiteExcedeDisponibilidadException.CodigoError, 422),
            (new JustificacionCasoLimiteInvalidaException(), JustificacionCasoLimiteInvalidaException.CodigoError, 422),
            (new CasoLimiteInvalidoException(), CasoLimiteInvalidoException.CodigoError, 422),
            (new DecisionCasoLimiteInvalidaPorDisponibilidadException(), DecisionCasoLimiteInvalidaPorDisponibilidadException.CodigoError, 422),
            (new PermisoDenegadoResolucionCasoLimiteException(), PermisoDenegadoResolucionCasoLimiteException.CodigoError, 403),
            (new ConflictoInteresDecisorCasoLimiteException(), ConflictoInteresDecisorCasoLimiteException.CodigoError, 403)
        };

        Assert.Equal("CASO_LIMITE_NO_DETECTADO", CasoLimiteNoDetectadoException.CodigoError);
        Assert.Equal("CASO_LIMITE_YA_RESUELTO", CasoLimiteYaResueltoException.CodigoError);
        Assert.Equal("SELECCION_CASO_LIMITE_INVALIDA", SeleccionCasoLimiteInvalidaException.CodigoError);
        Assert.Equal("SELECCION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD", SeleccionCasoLimiteExcedeDisponibilidadException.CodigoError);
        Assert.Equal("JUSTIFICACION_CASO_LIMITE_REQUERIDA", JustificacionCasoLimiteInvalidaException.CodigoError);
        Assert.Equal("CASO_LIMITE_INVALIDO", CasoLimiteInvalidoException.CodigoError);
        Assert.Equal("DECISION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD", DecisionCasoLimiteInvalidaPorDisponibilidadException.CodigoError);
        Assert.Equal("PERMISO_DENEGADO", PermisoDenegadoResolucionCasoLimiteException.CodigoError);
        Assert.Equal("CONFLICTO_INTERES_DECISOR", ConflictoInteresDecisorCasoLimiteException.CodigoError);

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
        const string mensajePersonalizado = "Detalle personalizado de la regla de caso límite.";

        var excepciones = new DomainException[]
        {
            new CasoLimiteNoDetectadoException(mensajePersonalizado),
            new CasoLimiteYaResueltoException(mensajePersonalizado),
            new SeleccionCasoLimiteInvalidaException(mensajePersonalizado),
            new SeleccionCasoLimiteExcedeDisponibilidadException(mensajePersonalizado),
            new JustificacionCasoLimiteInvalidaException(mensajePersonalizado),
            new CasoLimiteInvalidoException(mensajePersonalizado),
            new DecisionCasoLimiteInvalidaPorDisponibilidadException(mensajePersonalizado),
            new PermisoDenegadoResolucionCasoLimiteException(mensajePersonalizado),
            new ConflictoInteresDecisorCasoLimiteException(mensajePersonalizado)
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
            new CasoLimiteNoDetectadoException("   "),
            new CasoLimiteYaResueltoException("   "),
            new SeleccionCasoLimiteInvalidaException("   "),
            new SeleccionCasoLimiteExcedeDisponibilidadException("   "),
            new JustificacionCasoLimiteInvalidaException("   "),
            new CasoLimiteInvalidoException("   "),
            new DecisionCasoLimiteInvalidaPorDisponibilidadException("   "),
            new PermisoDenegadoResolucionCasoLimiteException("   "),
            new ConflictoInteresDecisorCasoLimiteException("   ")
        };

        foreach (var excepcion in excepciones)
        {
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }
}
