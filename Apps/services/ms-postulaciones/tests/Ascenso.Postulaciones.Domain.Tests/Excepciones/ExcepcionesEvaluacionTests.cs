using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesEvaluacionTests
{
    [Fact]
    public void Debe_ExponerCodigoEstadoYMensajePorDefecto_Cuando_SeCreanLasExcepcionesDeEvaluacion()
    {
        var noEncontradas = new (ExcepcionEntidadNoEncontrada Excepcion, string CodigoEsperado)[]
        {
            (new AsignacionEvaluacionNoEncontradaException(), AsignacionEvaluacionNoEncontradaException.CodigoError),
            (new EvaluacionNoEncontradaException(), EvaluacionNoEncontradaException.CodigoError)
        };

        var reglasNegocio = new (ExcepcionReglaNegocio Excepcion, string CodigoEsperado, int EstadoHttpEsperado)[]
        {
            (new EvaluacionConfidencialException(), EvaluacionConfidencialException.CodigoError, 403),
            (new AsignacionNoEvaluableException(), AsignacionNoEvaluableException.CodigoError, 409),
            (new EvaluacionYaEnviadaException(), EvaluacionYaEnviadaException.CodigoError, 409),
            (new EvaluacionInvalidadaException(), EvaluacionInvalidadaException.CodigoError, 409),
            (new EvaluacionDuplicadaException(), EvaluacionDuplicadaException.CodigoError, 409),
            (new PlazoEvaluacionVencidoException(), PlazoEvaluacionVencidoException.CodigoError, 409),
            (new PuntajeFueraDeRangoException(), PuntajeFueraDeRangoException.CodigoError, 422),
            (new CriterioNoPerteneceARubricaException(), CriterioNoPerteneceARubricaException.CodigoError, 422),
            (new EvaluacionIncompletaException(), EvaluacionIncompletaException.CodigoError, 422),
            (new EvaluacionInvalidaException(), EvaluacionInvalidaException.CodigoError, 422)
        };

        Assert.Equal("ASIGNACION_NO_ENCONTRADA", AsignacionEvaluacionNoEncontradaException.CodigoError);
        Assert.Equal("EVALUACION_NO_ENCONTRADA", EvaluacionNoEncontradaException.CodigoError);
        Assert.Equal("EVALUACION_CONFIDENCIAL", EvaluacionConfidencialException.CodigoError);
        Assert.Equal("ASIGNACION_NO_EVALUABLE", AsignacionNoEvaluableException.CodigoError);
        Assert.Equal("EVALUACION_YA_ENVIADA", EvaluacionYaEnviadaException.CodigoError);
        Assert.Equal("EVALUACION_INVALIDADA", EvaluacionInvalidadaException.CodigoError);
        Assert.Equal("EVALUACION_DUPLICADA", EvaluacionDuplicadaException.CodigoError);
        Assert.Equal("PLAZO_EVALUACION_VENCIDO", PlazoEvaluacionVencidoException.CodigoError);
        Assert.Equal("PUNTAJE_FUERA_DE_RANGO", PuntajeFueraDeRangoException.CodigoError);
        Assert.Equal("CRITERIO_NO_PERTENECE_A_RUBRICA", CriterioNoPerteneceARubricaException.CodigoError);
        Assert.Equal("EVALUACION_INCOMPLETA", EvaluacionIncompletaException.CodigoError);
        Assert.Equal("EVALUACION_INVALIDA", EvaluacionInvalidaException.CodigoError);

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
        const string mensajePersonalizado = "Detalle personalizado de la regla de evaluación.";

        var excepciones = new DomainException[]
        {
            new AsignacionEvaluacionNoEncontradaException(mensajePersonalizado),
            new EvaluacionNoEncontradaException(mensajePersonalizado),
            new EvaluacionConfidencialException(mensajePersonalizado),
            new AsignacionNoEvaluableException(mensajePersonalizado),
            new EvaluacionYaEnviadaException(mensajePersonalizado),
            new EvaluacionInvalidadaException(mensajePersonalizado),
            new EvaluacionDuplicadaException(mensajePersonalizado),
            new PlazoEvaluacionVencidoException(mensajePersonalizado),
            new PuntajeFueraDeRangoException(mensajePersonalizado),
            new CriterioNoPerteneceARubricaException(mensajePersonalizado),
            new EvaluacionIncompletaException(mensajePersonalizado),
            new EvaluacionInvalidaException(mensajePersonalizado)
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
            new AsignacionEvaluacionNoEncontradaException("   "),
            new EvaluacionNoEncontradaException("   "),
            new EvaluacionConfidencialException("   "),
            new AsignacionNoEvaluableException("   "),
            new EvaluacionYaEnviadaException("   "),
            new EvaluacionInvalidadaException("   "),
            new EvaluacionDuplicadaException("   "),
            new PlazoEvaluacionVencidoException("   "),
            new PuntajeFueraDeRangoException("   "),
            new CriterioNoPerteneceARubricaException("   "),
            new EvaluacionIncompletaException("   "),
            new EvaluacionInvalidaException("   ")
        };

        foreach (var excepcion in excepciones)
        {
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }

    [Fact]
    public void Debe_ConstruirMensajeDescriptivo_Cuando_SeUsaConstructorContextualDePuntajeYCompletitud()
    {
        var fueraDeRango = new PuntajeFueraDeRangoException("Mérito académico", 1.00m, 10.00m, 10.50m);
        var incompleta = new EvaluacionIncompletaException(2);

        Assert.Equal("PUNTAJE_FUERA_DE_RANGO", fueraDeRango.Codigo);
        Assert.Equal(422, fueraDeRango.EstadoHttp);
        Assert.Contains("Mérito académico", fueraDeRango.Message);
        Assert.Contains("1.00", fueraDeRango.Message);
        Assert.Contains("10.00", fueraDeRango.Message);
        Assert.Contains("10.50", fueraDeRango.Message);

        Assert.Equal("EVALUACION_INCOMPLETA", incompleta.Codigo);
        Assert.Equal(422, incompleta.EstadoHttp);
        Assert.Contains("2", incompleta.Message);
    }
}
