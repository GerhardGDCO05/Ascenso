using System.Reflection;

namespace Ascenso.Programas.Application.Tests.Configuracion;

public sealed class EnsambladoAplicacionTests
{
    [Fact]
    public void Debe_CargarElEnsambladoDeAplicacion_Cuando_SeEjecutanLasPruebas()
    {
        var ensamblado = Assembly.Load("Ascenso.Programas.Application");

        Assert.Equal("Ascenso.Programas.Application", ensamblado.GetName().Name);
    }
}

