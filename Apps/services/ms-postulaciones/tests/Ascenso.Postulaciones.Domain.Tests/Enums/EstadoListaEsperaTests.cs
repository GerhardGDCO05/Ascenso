using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class EstadoListaEsperaTests
{
    [Theory]
    [InlineData(EstadoListaEspera.EnEspera, "EN_ESPERA")]
    [InlineData(EstadoListaEspera.ElegiblePorVacante, "ELEGIBLE_POR_VACANTE")]
    [InlineData(EstadoListaEspera.Promovida, "PROMOVIDA")]
    public void Debe_ConvertirACodigoEnMayusculas_Cuando_SeSolicitaElCodigoDeEstadoListaEspera(
        EstadoListaEspera valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Theory]
    [InlineData("EN_ESPERA", EstadoListaEspera.EnEspera)]
    [InlineData("elegible_por_vacante", EstadoListaEspera.ElegiblePorVacante)]
    [InlineData(" PROMOVIDA ", EstadoListaEspera.Promovida)]
    public void Debe_ConvertirDesdeCodigo_Cuando_ElCodigoEsValido(
        string codigo,
        EstadoListaEspera esperado)
    {
        Assert.Equal(esperado, CodigoEnum.DesdeCodigo<EstadoListaEspera>(codigo));
    }

    [Fact]
    public void Debe_ConvertirIdaYVueltaTodosLosValoresDefinidos()
    {
        var valores = Enum.GetValues<EstadoListaEspera>();

        Assert.Equal(3, valores.Length);
        foreach (var valor in valores)
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<EstadoListaEspera>(valor.ACodigo()));
        }
    }

    [Fact]
    public void Debe_RechazarValoresYCodigosFueraDeCatalogo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((EstadoListaEspera)99).ACodigo());
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<EstadoListaEspera>("EXPIRADA"));
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<EstadoListaEspera>("   "));
    }
}
