using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class MotivoBloqueoRubricaTests
{
    [Theory]
    [InlineData(MotivoBloqueoRubrica.InicioEvaluacion, "INICIO_EVALUACION")]
    [InlineData(MotivoBloqueoRubrica.PrimeraCalificacion, "PRIMERA_CALIFICACION")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElMotivoDeBloqueo(
        MotivoBloqueoRubrica valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeMotivoBloqueoRubrica()
    {
        foreach (var valor in Enum.GetValues<MotivoBloqueoRubrica>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<MotivoBloqueoRubrica>(valor.ACodigo()));
        }
    }

    [Fact]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenAMotivoBloqueoRubrica()
    {
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<MotivoBloqueoRubrica>("MANUAL"));
    }
}
