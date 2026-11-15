using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesBaseTests
{
    [Fact]
    public void Debe_UsarEstatus422PorDefecto_Cuando_SeCreaUnaReglaDeNegocio()
    {
        var excepcion = new ExcepcionReglaNegocio("REGLA_INCUMPLIDA", "La regla no se cumple.");

        Assert.Equal("REGLA_INCUMPLIDA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal("La regla no se cumple.", excepcion.Message);
    }

    [Fact]
    public void Debe_PermitirUnEstatusPersonalizado_Cuando_SeCreaUnaReglaDeNegocio()
    {
        var excepcion = new ExcepcionReglaNegocio("CONFLICTO_NEGOCIO", "Conflicto.", 409);

        Assert.Equal(409, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_UsarEstatus404YCodigoPorDefecto_Cuando_NoSeEncuentraLaEntidad()
    {
        var excepcion = new ExcepcionEntidadNoEncontrada("El postulante no existe.");

        Assert.Equal(ExcepcionEntidadNoEncontrada.CodigoError, excepcion.Codigo);
        Assert.Equal(404, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_PermitirUnCodigoEspecifico_Cuando_NoSeEncuentraLaEntidad()
    {
        var excepcion = new ExcepcionEntidadNoEncontrada("No existe.", "POSTULANTE_NO_ENCONTRADO");

        Assert.Equal("POSTULANTE_NO_ENCONTRADO", excepcion.Codigo);
    }

    [Fact]
    public void Debe_UsarEstatus409YCodigoDeConcurrencia_Cuando_HayConflictoOptimista()
    {
        var excepcion = new ExcepcionConflictoConcurrencia();

        Assert.Equal(ExcepcionConflictoConcurrencia.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
    }
}
