using Ascenso.Postulaciones.Domain.Comun;
using NetArchTest.Rules;

namespace Ascenso.Postulaciones.ArchitectureTests.Arquitectura;

public sealed class DomainIndependenceTests
{
    private static readonly string[] _prefijosProhibidos =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "MassTransit",
        "Firebase",
        "Keycloak",
        "Npgsql"
    ];

    [Fact]
    public void Debe_NoReferenciarFrameworksDeInfraestructura_Cuando_SeInspeccionaElEnsambladoDeDominio()
    {
        var referencias = typeof(EntidadAuditable).Assembly.GetReferencedAssemblies();

        foreach (var prefijo in _prefijosProhibidos)
        {
            Assert.DoesNotContain(
                referencias,
                referencia => referencia.Name?.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase) == true);
        }

        var resultadoNetArch = Types.InAssembly(typeof(EntidadAuditable).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(_prefijosProhibidos)
            .GetResult();

        Assert.True(resultadoNetArch.IsSuccessful);
    }
}
