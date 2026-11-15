using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class DomainExceptionTests
{
    private sealed class ExcepcionPrueba : DomainException
    {
        public ExcepcionPrueba(string codigo, int estadoHttp, string mensaje)
            : base(codigo, estadoHttp, mensaje)
        {
        }
    }

    [Fact]
    public void Debe_ExponerCodigoYEstatusHttp_Cuando_SeConstruyeDomainException()
    {
        var excepcion = new ExcepcionPrueba("PROGRAMA_NO_CONFIGURABLE", 422, "Falta presupuesto.");

        Assert.Equal("PROGRAMA_NO_CONFIGURABLE", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal("Falta presupuesto.", excepcion.Message);
    }

    [Fact]
    public void Debe_RechazarParametrosInvalidos_Cuando_SeConstruyeDomainException()
    {
        Assert.Throws<ArgumentException>(() => new ExcepcionPrueba(" ", 400, "mensaje"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExcepcionPrueba("CODIGO", 399, "mensaje"));
    }
}

