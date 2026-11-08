using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Tests.ValueObjects;

public sealed class PoliticaCoberturaTests
{
    [Fact]
    public void Debe_CrearUnaPoliticaValida_Cuando_LosDatosEstanEnRango()
    {
        var politica = new PoliticaCobertura(TipoCobertura.Parcial, 75.50m, 0m, "USD");

        Assert.Equal(TipoCobertura.Parcial, politica.Modalidad);
        Assert.Equal(75.50m, politica.PorcentajeCobertura);
        Assert.Equal(0m, politica.MontoFijoPeriodo);
        Assert.Equal("USD", politica.Moneda);
    }

    [Fact]
    public void Debe_NormalizarLaMoneda_Cuando_SeIngresaEnMinusculas()
    {
        var politica = new PoliticaCobertura(TipoCobertura.Total, 100m, 0m, "usd");

        Assert.Equal("USD", politica.Moneda);
    }

    [Fact]
    public void Debe_TenerIgualdadEstructural_Cuando_LosValoresCoinciden()
    {
        var primera = new PoliticaCobertura(TipoCobertura.MontoFijo, 0m, 250.75m, "USD");
        var segunda = new PoliticaCobertura(TipoCobertura.MontoFijo, 0m, 250.75m, "usd");

        Assert.Equal(primera, segunda);
        Assert.Equal(primera.GetHashCode(), segunda.GetHashCode());
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Debe_RechazarPorcentajesFueraDeRango_Cuando_SeConstruye(decimal porcentaje)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PoliticaCobertura(TipoCobertura.Parcial, porcentaje, 0m, "USD"));
    }

    [Fact]
    public void Debe_RechazarMontoFijoNegativo_Cuando_SeConstruye()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PoliticaCobertura(TipoCobertura.MontoFijo, 0m, -1m, "USD"));
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarMonedasDistintasDeUSD_Cuando_SeConstruye(string moneda)
    {
        Assert.Throws<ArgumentException>(
            () => new PoliticaCobertura(TipoCobertura.Total, 100m, 0m, moneda));
    }

    [Fact]
    public void Debe_RechazarMasDeDosDecimales_Cuando_SeConstruye()
    {
        Assert.Throws<ArgumentException>(
            () => new PoliticaCobertura(TipoCobertura.Parcial, 75.555m, 0m, "USD"));
        Assert.Throws<ArgumentException>(
            () => new PoliticaCobertura(TipoCobertura.MontoFijo, 0m, 10.999m, "USD"));
    }

    [Fact]
    public void Debe_RechazarModalidadesInvalidas_Cuando_SeConstruye()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PoliticaCobertura((TipoCobertura)999, 50m, 0m, "USD"));
    }
}
