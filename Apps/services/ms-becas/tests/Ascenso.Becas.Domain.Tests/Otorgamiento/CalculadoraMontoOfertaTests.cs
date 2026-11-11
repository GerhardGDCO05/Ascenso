using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class CalculadoraMontoOfertaTests
{
    [Fact]
    public void Debe_CubrirLaMatriculaCompleta_Cuando_LaModalidadEsTotal()
    {
        var politica = PruebasOtorgamiento.Cobertura(TipoCobertura.Total, 100m);

        var monto = CalculadoraMontoOferta.CalcularMontoPorPeriodo(politica, 1234.56m);

        Assert.Equal(1234.56m, monto);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 500.28)]
    [InlineData(100, 1000.55)]
    public void Debe_AplicarElPorcentajeConRedondeoFinanciero_Cuando_LaModalidadEsParcial(
        decimal porcentaje,
        decimal esperado)
    {
        var politica = PruebasOtorgamiento.Cobertura(TipoCobertura.Parcial, porcentaje);

        var monto = CalculadoraMontoOferta.CalcularMontoPorPeriodo(politica, 1000.55m);

        Assert.Equal(esperado, monto);
    }

    [Fact]
    public void Debe_UsarElMontoFijo_Cuando_LaModalidadEsMontoFijo()
    {
        var politica = PruebasOtorgamiento.Cobertura(TipoCobertura.MontoFijo, 0m, 750.25m);

        var monto = CalculadoraMontoOferta.CalcularMontoPorPeriodo(politica, 5000m);

        Assert.Equal(750.25m, monto);
    }

    [Theory]
    [InlineData(TipoCobertura.Total)]
    [InlineData(TipoCobertura.Parcial)]
    [InlineData(TipoCobertura.MontoFijo)]
    public void Debe_RetornarCero_Cuando_LaMatriculaEsCero(TipoCobertura modalidad)
    {
        var politica = modalidad == TipoCobertura.MontoFijo
            ? PruebasOtorgamiento.Cobertura(TipoCobertura.MontoFijo, 0m, 0m)
            : PruebasOtorgamiento.Cobertura(modalidad, 50m);

        var monto = CalculadoraMontoOferta.CalcularMontoPorPeriodo(politica, 0m);

        Assert.Equal(0m, monto);
    }

    [Theory]
    [InlineData(500.25, 4, 2001.00)]
    [InlineData(333.335, 3, 1000.01)]
    [InlineData(0, 4, 0)]
    public void Debe_CalcularElTotalEstimado_ConRedondeoADosDecimales(
        decimal montoPorPeriodo,
        int duracion,
        decimal esperado)
    {
        var total = CalculadoraMontoOferta.CalcularMontoTotalEstimado(montoPorPeriodo, duracion);

        Assert.Equal(esperado, total);
    }

    [Fact]
    public void Debe_RechazarCostosNegativos_Cuando_SeCalculaElMonto()
    {
        var politica = PruebasOtorgamiento.Cobertura(TipoCobertura.Parcial, 50m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CalculadoraMontoOferta.CalcularMontoPorPeriodo(politica, -0.01m));
    }

    [Fact]
    public void Debe_RechazarCostosConMasDeDosDecimales_Cuando_SeCalculaElMonto()
    {
        var politica = PruebasOtorgamiento.Cobertura(TipoCobertura.Parcial, 50m);

        Assert.Throws<ArgumentException>(
            () => CalculadoraMontoOferta.CalcularMontoPorPeriodo(politica, 100.555m));
    }

    [Fact]
    public void Debe_RechazarDuracionesNoPositivas_Cuando_SeCalculaElTotal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CalculadoraMontoOferta.CalcularMontoTotalEstimado(100m, 0));
    }
}
