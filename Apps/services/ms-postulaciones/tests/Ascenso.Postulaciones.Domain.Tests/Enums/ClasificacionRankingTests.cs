using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class ClasificacionRankingTests
{
    [Theory]
    [InlineData(ClasificacionRanking.DentroDeCupo, "DENTRO_DE_CUPO")]
    [InlineData(ClasificacionRanking.ListaDeEspera, "LISTA_DE_ESPERA")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaLaClasificacionDeRanking(
        ClasificacionRanking valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeClasificacionRanking()
    {
        var valores = Enum.GetValues<ClasificacionRanking>();

        Assert.Equal(2, valores.Length);

        foreach (var valor in valores)
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<ClasificacionRanking>(valor.ACodigo()));
        }
    }

    [Theory]
    [InlineData("PROVISIONAL")]
    [InlineData("CONFIRMADO")]
    [InlineData("ADJUDICADO")]
    [InlineData("FUERA_DE_CUPO")]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenAClasificacionRanking(
        string codigoInvalido)
    {
        Assert.Throws<ArgumentException>(
            () => CodigoEnum.DesdeCodigo<ClasificacionRanking>(codigoInvalido));
    }

    [Fact]
    public void Debe_RechazarValorIndefinido_Cuando_SeConvierteACodigo()
    {
        var valorIndefinido = (ClasificacionRanking)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => valorIndefinido.ACodigo());
    }
}
