using Microsoft.AspNetCore.Mvc.Testing;

namespace Ascenso.Postulaciones.IntegrationTests;

public sealed class EnsambladoApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _fabrica;

    public EnsambladoApiTests(WebApplicationFactory<Program> fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public void Debe_CrearLaAplicacion_Cuando_SeComponeSinCadenaDeConexion()
    {
        using var cliente = _fabrica.CreateClient();

        Assert.NotNull(cliente);
        Assert.NotNull(cliente.BaseAddress);
    }
}
