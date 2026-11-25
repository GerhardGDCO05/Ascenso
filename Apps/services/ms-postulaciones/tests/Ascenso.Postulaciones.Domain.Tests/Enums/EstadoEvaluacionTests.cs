using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class EstadoEvaluacionTests
{
    [Theory]
    [InlineData(EstadoEvaluacion.Borrador, "BORRADOR")]
    [InlineData(EstadoEvaluacion.EnviadaDefinitiva, "ENVIADA_DEFINITIVA")]
    [InlineData(EstadoEvaluacion.InvalidadaPorConflicto, "INVALIDADA_POR_CONFLICTO")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElEstadoDeEvaluacion(
        EstadoEvaluacion valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeEstadoEvaluacion()
    {
        foreach (var valor in Enum.GetValues<EstadoEvaluacion>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<EstadoEvaluacion>(valor.ACodigo()));
        }
    }

    [Theory]
    [InlineData("PENDIENTE")]
    [InlineData("COMPLETADA")]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenAEstadoEvaluacion(
        string codigoInvalido)
    {
        Assert.Throws<ArgumentException>(
            () => CodigoEnum.DesdeCodigo<EstadoEvaluacion>(codigoInvalido));
    }

    [Fact]
    public void Debe_RechazarValorIndefinido_Cuando_SeConvierteACodigo()
    {
        var valorIndefinido = (EstadoEvaluacion)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => valorIndefinido.ACodigo());
    }
}
