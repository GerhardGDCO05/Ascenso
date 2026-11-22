using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class TipoEvaluadorTests
{
    [Theory]
    [InlineData(TipoEvaluador.Interno, "INTERNO")]
    [InlineData(TipoEvaluador.Externo, "EXTERNO")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElTipoDeEvaluador(
        TipoEvaluador valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeTipoEvaluador()
    {
        foreach (var valor in Enum.GetValues<TipoEvaluador>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<TipoEvaluador>(valor.ACodigo()));
        }
    }

    [Theory]
    [InlineData("MIXTO")]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenATipoEvaluador(string codigoInvalido)
    {
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<TipoEvaluador>(codigoInvalido));
    }
}
