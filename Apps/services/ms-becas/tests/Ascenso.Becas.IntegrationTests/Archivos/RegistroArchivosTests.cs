using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Archivos;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.IntegrationTests.Archivos;

public sealed class RegistroArchivosTests
{
    [Fact]
    public void Debe_UsarAlmacenamientoLocal_Cuando_NoSeSeleccionaFirebase()
    {
        var servicios = new ServiceCollection();
        servicios.AgregarArchivos(new ConfigurationBuilder().Build());

        using var proveedor = servicios.BuildServiceProvider();

        proveedor.GetRequiredService<IAlmacenamientoArchivos>()
            .Should()
            .BeOfType<AlmacenamientoLocalArchivos>();
    }

    [Fact]
    public void Debe_FallarExplicitamente_Cuando_FirebaseCloudNoTieneCredenciales()
    {
        var rutaInexistente = Path.Combine(
            Path.GetTempPath(),
            $"credencial-firebase-inexistente-{Guid.NewGuid():N}.json");
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Firebase",
                ["Storage:Firebase:Bucket"] = "bucket-pruebas",
                ["Storage:Firebase:RutaCredenciales"] = rutaInexistente
            })
            .Build();
        var servicios = new ServiceCollection();

        var accion = () => servicios.AgregarArchivos(configuracion);

        accion.Should().Throw<FileNotFoundException>();
    }
}
