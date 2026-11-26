using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class EstadoVentanaRevisionTests
{
    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Borrador, "BORRADOR")]
    [InlineData(EstadoConvocatoriaExterna.Publicada, "PUBLICADA")]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, "CERRADA")]
    [InlineData(EstadoConvocatoriaExterna.Cancelada, "CANCELADA")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElEstadoConvocatoriaExterna(
        EstadoConvocatoriaExterna valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeEstadoConvocatoriaExterna()
    {
        var valores = Enum.GetValues<EstadoConvocatoriaExterna>();

        Assert.Equal(4, valores.Length);

        foreach (var valor in valores)
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<EstadoConvocatoriaExterna>(valor.ACodigo()));
        }
    }

    [Theory]
    [InlineData("ABIERTA")]
    [InlineData("FINALIZADA")]
    [InlineData("EN_REVISION")]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenAEstadoConvocatoriaExterna(
        string codigoInvalido)
    {
        Assert.Throws<ArgumentException>(
            () => CodigoEnum.DesdeCodigo<EstadoConvocatoriaExterna>(codigoInvalido));
    }

    [Fact]
    public void Debe_RechazarValorIndefinido_Cuando_SeConvierteEstadoConvocatoriaExternaACodigo()
    {
        var valorIndefinido = (EstadoConvocatoriaExterna)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => valorIndefinido.ACodigo());
    }

    [Theory]
    [InlineData(EstadoPeriodoRevision.NoIniciado, "NO_INICIADO")]
    [InlineData(EstadoPeriodoRevision.Abierto, "ABIERTO")]
    [InlineData(EstadoPeriodoRevision.Cerrado, "CERRADO")]
    [InlineData(EstadoPeriodoRevision.Cancelado, "CANCELADO")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElEstadoPeriodoRevision(
        EstadoPeriodoRevision valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeEstadoPeriodoRevision()
    {
        var valores = Enum.GetValues<EstadoPeriodoRevision>();

        Assert.Equal(4, valores.Length);

        foreach (var valor in valores)
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<EstadoPeriodoRevision>(valor.ACodigo()));
        }
    }

    [Theory]
    [InlineData("PUBLICADA")]
    [InlineData("BORRADOR")]
    [InlineData("PENDIENTE")]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenAEstadoPeriodoRevision(
        string codigoInvalido)
    {
        Assert.Throws<ArgumentException>(
            () => CodigoEnum.DesdeCodigo<EstadoPeriodoRevision>(codigoInvalido));
    }

    [Fact]
    public void Debe_RechazarValorIndefinido_Cuando_SeConvierteEstadoPeriodoRevisionACodigo()
    {
        var valorIndefinido = (EstadoPeriodoRevision)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => valorIndefinido.ACodigo());
    }
}
