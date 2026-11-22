using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesRubricaTests
{
    [Fact]
    public void Debe_ExponerCodigoYEstatus_Cuando_SeCreanLasExcepcionesDeRubrica()
    {
        var casos = new (DomainException Excepcion, string Codigo, int EstadoHttp)[]
        {
            (new RubricaEvaluacionInvalidaException("Rúbrica inválida."), RubricaEvaluacionInvalidaException.CodigoError, 400),
            (new CriterioEvaluacionInvalidoException("Criterio inválido."), CriterioEvaluacionInvalidoException.CodigoError, 400),
            (new PesosInvalidosException(90.00m), PesosInvalidosException.CodigoError, 422),
            (new RubricaBloqueadaException(), RubricaBloqueadaException.CodigoError, 409),
            (new RubricaNoPublicadaException(), RubricaNoPublicadaException.CodigoError, 409),
            (new RubricaDuplicadaException(), RubricaDuplicadaException.CodigoError, 409),
            (new RubricaNoEncontradaException(), RubricaNoEncontradaException.CodigoError, 404),
            (new CriterioNoEncontradoException(), CriterioNoEncontradoException.CodigoError, 404)
        };

        foreach (var (excepcion, codigo, estadoHttp) in casos)
        {
            Assert.IsAssignableFrom<ExcepcionReglaNegocio>(excepcion);
            Assert.Equal(codigo, excepcion.Codigo);
            Assert.Equal(estadoHttp, excepcion.EstadoHttp);
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }

    [Fact]
    public void Debe_ExponerMensajeDePublicada_Cuando_SeUsaLaFabricaPublicada()
    {
        var excepcion = RubricaBloqueadaException.Publicada();

        Assert.Equal("RUBRICA_BLOQUEADA", excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal(
            "La rúbrica está publicada y no admite modificaciones; los cambios requieren una rúbrica nueva.",
            excepcion.Message);
    }

    [Theory]
    [InlineData(MotivoBloqueoRubrica.InicioEvaluacion, "inicio de la etapa de evaluación")]
    [InlineData(MotivoBloqueoRubrica.PrimeraCalificacion, "registro de la primera calificación")]
    public void Debe_ExponerFechaUtcYMotivoLegible_Cuando_SeUsaLaFabricaBloqueada(
        MotivoBloqueoRubrica motivo,
        string motivoLegibleEsperado)
    {
        var fechaConOffset = new DateTimeOffset(2026, 11, 22, 10, 30, 45, TimeSpan.FromHours(-4));

        var excepcion = RubricaBloqueadaException.Bloqueada(fechaConOffset, motivo);

        Assert.Equal("RUBRICA_BLOQUEADA", excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal(
            $"La rúbrica está bloqueada desde 2026-11-22 14:30:45 UTC por {motivoLegibleEsperado}; los cambios requieren una rúbrica nueva.",
            excepcion.Message);
    }

    [Fact]
    public void Debe_RechazarMotivoNoDefinido_Cuando_SeUsaLaFabricaBloqueada()
    {
        var fecha = new DateTimeOffset(2026, 11, 22, 14, 30, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => RubricaBloqueadaException.Bloqueada(fecha, (MotivoBloqueoRubrica)999));
    }

    [Fact]
    public void Debe_ExponerCodigo409YMensajeExplicativo_Cuando_SeCreaRubricaNoPublicadaException()
    {
        var excepcion = new RubricaNoPublicadaException();

        Assert.IsAssignableFrom<ExcepcionReglaNegocio>(excepcion);
        Assert.Equal("RUBRICA_NO_PUBLICADA", excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal(
            "Solo una rúbrica publicada puede bloquearse; la rúbrica está en borrador.",
            excepcion.Message);
    }

    [Fact]
    public void Debe_IndicarFaltanteEnElMensaje_Cuando_LaSumaDePesosEsMenorQue100()
    {
        var excepcion = new PesosInvalidosException(99.99m);

        Assert.Equal("PESOS_INVALIDOS", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal(99.99m, excepcion.SumaPesos);
        Assert.Equal(0.01m, excepcion.DiferenciaPara100);
        Assert.Contains("faltan 0.01", excepcion.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Debe_IndicarExcesoEnElMensaje_Cuando_LaSumaDePesosEsMayorQue100()
    {
        var excepcion = new PesosInvalidosException(100.01m);

        Assert.Equal("PESOS_INVALIDOS", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal(100.01m, excepcion.SumaPesos);
        Assert.Equal(-0.01m, excepcion.DiferenciaPara100);
        Assert.Contains("excede por 0.01", excepcion.Message, StringComparison.Ordinal);
    }
}
