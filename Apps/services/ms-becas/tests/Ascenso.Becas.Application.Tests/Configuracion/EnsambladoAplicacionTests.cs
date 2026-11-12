using System.Reflection;

namespace Ascenso.Becas.Application.Tests.Configuracion;

public sealed class EnsambladoAplicacionTests
{
    [Fact]
    public void Debe_CargarElEnsambladoDeAplicacion_Cuando_SeEjecutanLasPruebas()
    {
        var ensamblado = Assembly.Load("Ascenso.Becas.Application");

        Assert.Equal("Ascenso.Becas.Application", ensamblado.GetName().Name);
    }
}
