using System.Reflection;
using Ascenso.Postulaciones.Domain.Comun;

namespace Ascenso.Postulaciones.ArchitectureTests.Arquitectura;

public sealed class EncapsulamientoTests
{
    private const BindingFlags PropiedadesDeInstancia =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    public void Debe_NoExponerSettersPublicos_Cuando_SeInspeccionanLasEntidadesDelDominio()
    {
        var tiposEntidad = EnsambladoDominio()
            .GetTypes()
            .Where(tipo => tipo.IsClass && !tipo.IsAbstract && EsEntidad(tipo));

        foreach (var tipo in tiposEntidad)
        {
            var propiedadesConSetterPublico = tipo
                .GetProperties(PropiedadesDeInstancia)
                .Where(propiedad => propiedad.SetMethod?.IsPublic == true);

            Assert.Empty(propiedadesConSetterPublico);
        }
    }

    [Fact]
    public void Debe_NoExponerColeccionesMutables_Cuando_SeInspeccionanLasPropiedadesPublicas()
    {
        var tiposMutables = new[]
        {
            typeof(List<>),
            typeof(Dictionary<,>),
            typeof(HashSet<>)
        };

        var propiedadesExpuestas = EnsambladoDominio()
            .GetTypes()
            .Where(tipo => tipo.IsPublic)
            .SelectMany(tipo => tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(propiedad => tiposMutables.Contains(propiedad.PropertyType.GetGenericTypeDefinitionOrSelf()));

        Assert.Empty(propiedadesExpuestas);
    }

    private static bool EsEntidad(Type tipo)
        => typeof(EntidadInmutable).IsAssignableFrom(tipo)
            || EsSubtipoDeEntidadAuditable(tipo)
            || typeof(ValueObject).IsAssignableFrom(tipo);

    private static bool EsSubtipoDeEntidadAuditable(Type tipo)
    {
        for (var actual = tipo.BaseType; actual is not null; actual = actual.BaseType)
        {
            if (actual.IsGenericType
                && actual.GetGenericTypeDefinition() == typeof(EntidadAuditable<>))
            {
                return true;
            }
        }

        return false;
    }

    private static Assembly EnsambladoDominio() => typeof(EntidadAuditable).Assembly;
}

internal static class TipoExtensions
{
    public static Type GetGenericTypeDefinitionOrSelf(this Type tipo)
        => tipo.IsGenericType ? tipo.GetGenericTypeDefinition() : tipo;
}
