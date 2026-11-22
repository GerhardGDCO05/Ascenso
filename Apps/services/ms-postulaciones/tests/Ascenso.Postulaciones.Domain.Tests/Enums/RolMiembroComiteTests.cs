using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class RolMiembroComiteTests
{
    [Theory]
    [InlineData(RolMiembroComite.Jefatura, "JEFATURA")]
    [InlineData(RolMiembroComite.Evaluador, "EVALUADOR")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElRolDeMiembroComite(
        RolMiembroComite valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeRolMiembroComite()
    {
        foreach (var valor in Enum.GetValues<RolMiembroComite>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<RolMiembroComite>(valor.ACodigo()));
        }
    }

    [Theory]
    [InlineData("COORDINADOR")]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenARolMiembroComite(string codigoInvalido)
    {
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<RolMiembroComite>(codigoInvalido));
    }
}
