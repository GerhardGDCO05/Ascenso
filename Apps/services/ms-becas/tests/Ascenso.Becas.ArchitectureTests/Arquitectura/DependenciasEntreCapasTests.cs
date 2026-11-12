using System.Reflection;

namespace Ascenso.Becas.ArchitectureTests.Arquitectura;

public sealed class DependenciasEntreCapasTests
{
    private const string Dominio = "Ascenso.Becas.Domain";
    private const string Aplicacion = "Ascenso.Becas.Application";
    private const string Infraestructura = "Ascenso.Becas.Infrastructure";
    private const string Api = "Ascenso.Becas.Api";

    [Theory]
    [InlineData(Dominio, Aplicacion)]
    [InlineData(Dominio, Infraestructura)]
    [InlineData(Dominio, Api)]
    [InlineData(Aplicacion, Infraestructura)]
    [InlineData(Aplicacion, Api)]
    [InlineData(Infraestructura, Api)]
    public void Debe_NoReferenciarUnaCapaExterior_Cuando_SeInspeccionanLasReferencias(
        string capa,
        string capaProhibida)
    {
        var referencias = Assembly.Load(capa).GetReferencedAssemblies();

        Assert.DoesNotContain(
            referencias,
            referencia => string.Equals(referencia.Name, capaProhibida, StringComparison.OrdinalIgnoreCase));
    }
}
