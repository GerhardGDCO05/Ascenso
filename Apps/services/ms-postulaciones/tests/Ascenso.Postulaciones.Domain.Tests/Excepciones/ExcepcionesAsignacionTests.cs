using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesAsignacionTests
{
    [Fact]
    public void Debe_ExponerCodigoEstadoYMensajePorDefecto_Cuando_SeCreanLasExcepcionesDeAsignacion()
    {
        var casos = new (DomainException Excepcion, string CodigoEsperado, int EstadoHttpEsperado)[]
        {
            (new AsignacionEvaluacionInvalidaException(), AsignacionEvaluacionInvalidaException.CodigoError, 422),
            (new EvaluadoresInsuficientesException(), EvaluadoresInsuficientesException.CodigoError, 422),
            (new EvaluadorNoMiembroActivoException(), EvaluadorNoMiembroActivoException.CodigoError, 422),
            (new ComiteInactivoException(), ComiteInactivoException.CodigoError, 422),
            (new AsignacionDuplicadaException(), AsignacionDuplicadaException.CodigoError, 409)
        };

        Assert.Equal("ASIGNACION_INVALIDA", AsignacionEvaluacionInvalidaException.CodigoError);
        Assert.Equal("EVALUACIONES_INSUFICIENTES", EvaluadoresInsuficientesException.CodigoError);
        Assert.Equal("EVALUADOR_NO_MIEMBRO_ACTIVO", EvaluadorNoMiembroActivoException.CodigoError);
        Assert.Equal("COMITE_INACTIVO", ComiteInactivoException.CodigoError);
        Assert.Equal("ASIGNACION_DUPLICADA", AsignacionDuplicadaException.CodigoError);

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
        const string mensajePersonalizado = "Detalle personalizado de la regla de negocio de asignación.";

        var excepciones = new ExcepcionReglaNegocio[]
        {
            new AsignacionEvaluacionInvalidaException(mensajePersonalizado),
            new EvaluadoresInsuficientesException(mensajePersonalizado),
            new EvaluadorNoMiembroActivoException(mensajePersonalizado),
            new ComiteInactivoException(mensajePersonalizado),
            new AsignacionDuplicadaException(mensajePersonalizado)
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
            new AsignacionEvaluacionInvalidaException("   "),
            new EvaluadoresInsuficientesException("   "),
            new EvaluadorNoMiembroActivoException("   "),
            new ComiteInactivoException("   "),
            new AsignacionDuplicadaException("   ")
        };

        foreach (var excepcion in excepciones)
        {
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }
}
