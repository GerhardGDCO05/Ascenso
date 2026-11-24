using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class EstadoAsignacionEvaluacionTests
{
    [Theory]
    [InlineData(EstadoAsignacionEvaluacion.Pendiente, "PENDIENTE")]
    [InlineData(EstadoAsignacionEvaluacion.EnProgreso, "EN_PROGRESO")]
    [InlineData(EstadoAsignacionEvaluacion.Completada, "COMPLETADA")]
    [InlineData(EstadoAsignacionEvaluacion.ReasignadaPorConflicto, "REASIGNADA_POR_CONFLICTO")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElEstadoDeAsignacionEvaluacion(
        EstadoAsignacionEvaluacion valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeEstadoAsignacionEvaluacion()
    {
        foreach (var valor in Enum.GetValues<EstadoAsignacionEvaluacion>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<EstadoAsignacionEvaluacion>(valor.ACodigo()));
        }
    }

    [Theory]
    [InlineData("CANCELADA")]
    [InlineData("ASIGNADA")]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenAEstadoAsignacionEvaluacion(
        string codigoInvalido)
    {
        Assert.Throws<ArgumentException>(
            () => CodigoEnum.DesdeCodigo<EstadoAsignacionEvaluacion>(codigoInvalido));
    }

    [Fact]
    public void Debe_RechazarValorIndefinido_Cuando_SeConvierteACodigo()
    {
        var valorIndefinido = (EstadoAsignacionEvaluacion)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => valorIndefinido.ACodigo());
    }
}
