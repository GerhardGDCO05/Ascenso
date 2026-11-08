using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Tests.ValueObjects;

public sealed class CondicionesPermanenciaTests
{
    [Fact]
    public void Debe_CrearCondicionesValidas_Cuando_LosDatosEstanEnRango()
    {
        var condiciones = new CondicionesPermanencia(
            14.50m,
            4,
            1,
            80,
            ConsecuenciaPermanencia.Suspension);

        Assert.Equal(14.50m, condiciones.PromedioMinimoRenovacion);
        Assert.Equal(4, condiciones.MateriasMinimasAprobadas);
        Assert.Equal(1, condiciones.MaximoMateriasReprobadas);
        Assert.Equal(80, condiciones.HorasServicioPorPeriodo);
        Assert.Equal(ConsecuenciaPermanencia.Suspension, condiciones.ConsecuenciaIncumplimiento);
    }

    [Fact]
    public void Debe_TenerIgualdadEstructural_Cuando_LosValoresCoinciden()
    {
        var primera = new CondicionesPermanencia(14.50m, 4, 1, 80, ConsecuenciaPermanencia.Revocacion);
        var segunda = new CondicionesPermanencia(14.50m, 4, 1, 80, ConsecuenciaPermanencia.Revocacion);

        Assert.Equal(primera, segunda);
        Assert.Equal(primera.GetHashCode(), segunda.GetHashCode());
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(20.01)]
    public void Debe_RechazarPromediosFueraDeRango_Cuando_SeConstruye(decimal promedio)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CondicionesPermanencia(promedio, 4, 1, 80, ConsecuenciaPermanencia.Suspension));
    }

    [Fact]
    public void Debe_RechazarPromediosConMasDeDosDecimales_Cuando_SeConstruye()
    {
        Assert.Throws<ArgumentException>(
            () => new CondicionesPermanencia(14.555m, 4, 1, 80, ConsecuenciaPermanencia.Suspension));
    }

    [Fact]
    public void Debe_RechazarCantidadesNegativas_Cuando_SeConstruye()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CondicionesPermanencia(14m, -1, 1, 80, ConsecuenciaPermanencia.Suspension));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CondicionesPermanencia(14m, 4, -1, 80, ConsecuenciaPermanencia.Suspension));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CondicionesPermanencia(14m, 4, 1, -1, ConsecuenciaPermanencia.Suspension));
    }

    [Fact]
    public void Debe_RechazarConsecuenciasInvalidas_Cuando_SeConstruye()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CondicionesPermanencia(14m, 4, 1, 80, (ConsecuenciaPermanencia)999));
    }
}
