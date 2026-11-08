using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Domain.Tests.Arquitectura;

public sealed class DomainIndependenceTests
{
    [Fact]
    public void Debe_NoReferenciarFrameworksDeInfraestructura_Cuando_SeInspeccionaElEnsambladoDeDominio()
    {
        var prefijosProhibidos = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "MassTransit",
            "Firebase",
            "Keycloak",
            "Npgsql"
        };

        var referencias = typeof(EntidadAuditable).Assembly.GetReferencedAssemblies();

        foreach (var prefijo in prefijosProhibidos)
        {
            Assert.DoesNotContain(
                referencias,
                referencia => referencia.Name?.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase) == true);
        }
    }

    [Fact]
    public void Debe_ExponerLosPuertosComoInterfaces_Cuando_SeInspeccionaElEnsambladoDeDominio()
    {
        Type[] puertos =
        [
            typeof(IBecaRepository),
            typeof(IOfertaBecaRepository),
            typeof(ICompromisoBecarioRepository),
            typeof(IUnitOfWork),
            typeof(IClock),
            typeof(IAlmacenamientoArchivos),
            typeof(ICartaOfertaPdfGenerator),
            typeof(IPostulacionesClient),
            typeof(IProgramasClient),
            typeof(IOtpClient)
        ];

        Assert.All(puertos, puerto => Assert.True(puerto.IsInterface, $"{puerto.Name} debe ser una interfaz."));
    }
}
