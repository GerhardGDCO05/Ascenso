using System.Reflection;
using Ascenso.Programas.Domain.Comun;
using NetArchTest.Rules;

namespace Ascenso.Programas.ArchitectureTests.Arquitectura;

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

    [Fact]
    public void Debe_NoExponerSettersPublicosEnEntidadesMutables_Cuando_SeInspeccionaElDominio()
    {
        var tiposDominio = typeof(EntidadAuditable).Assembly
            .GetTypes()
            .Where(t => t.IsClass && (typeof(EntidadAuditable).IsAssignableFrom(t)
                || typeof(EntidadInmutable).IsAssignableFrom(t)));

        foreach (var tipo in tiposDominio)
        {
            var propiedadesConSetterPublico = tipo
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod?.IsPublic == true);

            Assert.Empty(propiedadesConSetterPublico);
        }
    }
}

