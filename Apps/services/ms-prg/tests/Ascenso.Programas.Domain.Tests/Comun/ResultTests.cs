using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Tests.Comun;

public sealed class ResultTests
{
    [Fact]
    public void Debe_RepresentarExitoConValor_Cuando_SeCreaResultadoExitoso()
    {
        var resultado = Result.Exito(42);

        Assert.True(resultado.EsExitoso);
        Assert.False(resultado.EsFallo);
        Assert.Equal(42, resultado.Valor);
        Assert.Null(resultado.CodigoError);
        Assert.Null(resultado.MensajeError);
    }

    [Fact]
    public void Debe_RepresentarFalloYLanzarAlLeerValor_Cuando_SeCreaResultadoFallido()
    {
        var resultado = Result.Fallo<int>("SIN_CUPO", "No hay cupos disponibles.");

        Assert.False(resultado.EsExitoso);
        Assert.True(resultado.EsFallo);
        Assert.Equal("SIN_CUPO", resultado.CodigoError);
        Assert.Equal("No hay cupos disponibles.", resultado.MensajeError);
        Assert.Throws<InvalidOperationException>(() => resultado.Valor);
    }

    [Fact]
    public void Debe_RechazarCodigoErrorVacio_Cuando_SeCreaResultadoFallido()
    {
        Assert.Throws<ArgumentException>(() => Result.Fallo(" "));
    }
}

