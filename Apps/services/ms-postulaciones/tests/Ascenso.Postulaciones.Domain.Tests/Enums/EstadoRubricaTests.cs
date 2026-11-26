using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class EstadoRubricaTests
{
    [Theory]
    [InlineData(EstadoRubrica.Borrador, "BORRADOR")]
    [InlineData(EstadoRubrica.Publicada, "PUBLICADA")]
    [InlineData(EstadoRubrica.Bloqueada, "BLOQUEADA")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElEstadoDeLaRubrica(
        EstadoRubrica valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeEstadoRubrica()
    {
        foreach (var valor in Enum.GetValues<EstadoRubrica>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<EstadoRubrica>(valor.ACodigo()));
        }
    }

    [Fact]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenAEstadoRubrica()
    {
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<EstadoRubrica>("ARCHIVADA"));
    }
}
