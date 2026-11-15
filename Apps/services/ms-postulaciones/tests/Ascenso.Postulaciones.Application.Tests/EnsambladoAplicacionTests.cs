using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests;

public sealed class EnsambladoAplicacionTests
{
    [Fact]
    public void Debe_ExponerElEnsambladoDeAplicacion_Cuando_SeCargaElProyecto()
    {
        var ensamblado = typeof(DependencyInjection).Assembly;

        ensamblado.GetName().Name.Should().Be("Ascenso.Postulaciones.Application");
        ensamblado.GetTypes().Should().Contain(typeof(DependencyInjection));
    }
}
