using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class EstadoRankingTests
{
    [Theory]
    [InlineData(EstadoRanking.Provisional, "PROVISIONAL")]
    [InlineData(EstadoRanking.Confirmado, "CONFIRMADO")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElEstadoDeRanking(
        EstadoRanking valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeEstadoRanking()
    {
        var valores = Enum.GetValues<EstadoRanking>();

        Assert.Equal(2, valores.Length);

        foreach (var valor in valores)
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<EstadoRanking>(valor.ACodigo()));
        }
    }

    [Theory]
    [InlineData("BORRADOR")]
    [InlineData("ENVIADA_DEFINITIVA")]
    [InlineData("PUBLICADO")]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenAEstadoRanking(
        string codigoInvalido)
    {
        Assert.Throws<ArgumentException>(
            () => CodigoEnum.DesdeCodigo<EstadoRanking>(codigoInvalido));
    }

    [Fact]
    public void Debe_RechazarValorIndefinido_Cuando_SeConvierteACodigo()
    {
        var valorIndefinido = (EstadoRanking)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => valorIndefinido.ACodigo());
    }
}
