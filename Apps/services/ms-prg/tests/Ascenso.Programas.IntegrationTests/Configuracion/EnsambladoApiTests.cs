using System.Reflection;

namespace Ascenso.Programas.IntegrationTests.Configuracion;

public sealed class EnsambladoApiTests
{
    [Fact]
    public void Debe_ExponerUnPuntoDeEntrada_Cuando_SeInspeccionaElEnsambladoDeLaApi()
    {
        var ensamblado = Assembly.Load("Ascenso.Programas.Api");

        Assert.NotNull(ensamblado.EntryPoint);
    }
}

