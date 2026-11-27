using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.IntegrationTests.Adapters;

public sealed class VerificadorConflictoInteresDecisorSinOperacionTests
{
    [Fact]
    public async Task Debe_RetornarFalse_Cuando_LosArgumentosSonValidos()
    {
        var verificador = new VerificadorConflictoInteresDecisorSinOperacion();

        var resultado = await verificador.TieneConflictoEnPostulacionesAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [Guid.NewGuid(), Guid.NewGuid()]);

        resultado.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_LosArgumentosSonInvalidos()
    {
        var verificador = new VerificadorConflictoInteresDecisorSinOperacion();

        var convocatoriaVacia = () => verificador.TieneConflictoEnPostulacionesAsync(
            Guid.Empty,
            Guid.NewGuid(),
            [Guid.NewGuid()]);
        var usuarioVacio = () => verificador.TieneConflictoEnPostulacionesAsync(
            Guid.NewGuid(),
            Guid.Empty,
            [Guid.NewGuid()]);
        var postulacionesNulas = () => verificador.TieneConflictoEnPostulacionesAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null!);
        var postulacionesVacias = () => verificador.TieneConflictoEnPostulacionesAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            []);
        var postulacionConGuidEmpty = () => verificador.TieneConflictoEnPostulacionesAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [Guid.Empty]);

        await convocatoriaVacia.Should().ThrowAsync<ArgumentException>();
        await usuarioVacio.Should().ThrowAsync<ArgumentException>();
        await postulacionesNulas.Should().ThrowAsync<ArgumentNullException>();
        await postulacionesVacias.Should().ThrowAsync<ArgumentException>();
        await postulacionConGuidEmpty.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_RegistrarVerificadorConflictoInteresDecisorSinOperacionComoScoped_Cuando_SeConfiguraInfraestructura()
    {
        var servicios = new ServiceCollection();

        servicios.AgregarInfraestructuraPostulaciones(
            PostulacionesDbContextFactory.CadenaConexionPorDefecto);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IVerificadorConflictoInteresDecisor)
            && d.ImplementationType == typeof(VerificadorConflictoInteresDecisorSinOperacion)
            && d.Lifetime == ServiceLifetime.Scoped);

        using var proveedor = servicios.BuildServiceProvider();
        using var scope = proveedor.CreateScope();

        var verificador = scope.ServiceProvider.GetRequiredService<IVerificadorConflictoInteresDecisor>();
        verificador.Should().BeOfType<VerificadorConflictoInteresDecisorSinOperacion>();
    }
}
