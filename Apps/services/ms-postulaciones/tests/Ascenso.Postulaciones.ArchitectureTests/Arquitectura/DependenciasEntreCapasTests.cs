using System.Reflection;
using Ascenso.Postulaciones.Domain.Comun;
using NetArchTest.Rules;

namespace Ascenso.Postulaciones.ArchitectureTests.Arquitectura;

public sealed class DependenciasEntreCapasTests
{
    private const string Dominio = "Ascenso.Postulaciones.Domain";
    private const string Aplicacion = "Ascenso.Postulaciones.Application";
    private const string Infraestructura = "Ascenso.Postulaciones.Infrastructure";
    private const string Api = "Ascenso.Postulaciones.Api";

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

    [Fact]
    public void Debe_CumplirReglaNetArchTestDeDominio_Cuando_SeEvaluaLaArquitecturaHexagonal()
    {
        var resultado = Types.InAssembly(typeof(EntidadAuditable).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(Aplicacion, Infraestructura, Api)
            .GetResult();

        Assert.True(resultado.IsSuccessful);
    }
}
