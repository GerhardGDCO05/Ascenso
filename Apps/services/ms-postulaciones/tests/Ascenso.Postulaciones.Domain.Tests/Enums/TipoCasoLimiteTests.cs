using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class TipoCasoLimiteTests
{
    [Theory]
    [InlineData(TipoCasoLimite.EmpatePorCupo, "EMPATE_POR_CUPO")]
    [InlineData(TipoCasoLimite.EmpatePorPresupuesto, "EMPATE_POR_PRESUPUESTO")]
    [InlineData(TipoCasoLimite.CasoLimitePresupuestario, "CASO_LIMITE_PRESUPUESTARIO")]
    public void Debe_ConvertirACodigoEnMayusculas_Cuando_SeSolicitaElCodigoDeTipoCasoLimite(
        TipoCasoLimite valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Theory]
    [InlineData("EMPATE_POR_CUPO", TipoCasoLimite.EmpatePorCupo)]
    [InlineData("empate_por_presupuesto", TipoCasoLimite.EmpatePorPresupuesto)]
    [InlineData(" CASO_LIMITE_PRESUPUESTARIO ", TipoCasoLimite.CasoLimitePresupuestario)]
    public void Debe_ConvertirDesdeCodigo_Cuando_ElCodigoEsValido(
        string codigo,
        TipoCasoLimite esperado)
    {
        Assert.Equal(esperado, CodigoEnum.DesdeCodigo<TipoCasoLimite>(codigo));
    }

    [Fact]
    public void Debe_ConvertirIdaYVueltaTodosLosValoresDefinidos()
    {
        var valores = Enum.GetValues<TipoCasoLimite>();

        Assert.Equal(3, valores.Length);
        foreach (var valor in valores)
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<TipoCasoLimite>(valor.ACodigo()));
        }
    }

    [Fact]
    public void Debe_RechazarValoresYCodigosFueraDeCatalogo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((TipoCasoLimite)99).ACodigo());
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<TipoCasoLimite>("OTRO_CASO"));
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<TipoCasoLimite>("   "));
    }
}
