using System.Reflection;

namespace Ascenso.Becas.IntegrationTests.Configuracion;

public sealed class EnsambladoApiTests
{
    [Fact]
    public void Debe_ExponerUnPuntoDeEntrada_Cuando_SeInspeccionaElEnsambladoDeLaApi()
    {
        var ensamblado = Assembly.Load("Ascenso.Becas.Api");

        Assert.NotNull(ensamblado.EntryPoint);
    }
}
