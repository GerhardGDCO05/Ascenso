using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;

namespace Ascenso.Postulaciones.Domain.Tests.Postulantes.ObjetosValor;

public sealed class DatosMiembroFamiliarTests
{
    [Fact]
    public void Debe_CrearElObjetoDeValor_Cuando_LosDatosSonValidos()
    {
        var datos = new DatosMiembroFamiliar(
            Parentesco.Madre,
            DatosMiembroFamiliar.EdadMaxima,
            Ocupacion.LaboresHogar,
            0m);

        Assert.Equal(Parentesco.Madre, datos.Parentesco);
        Assert.Equal(DatosMiembroFamiliar.EdadMaxima, datos.Edad);
        Assert.Equal(Ocupacion.LaboresHogar, datos.Ocupacion);
        Assert.Equal(0m, datos.IngresoMensual);
    }

    [Fact]
    public void Debe_SerIgualPorValor_Cuando_LosDatosCoinciden()
    {
        var primero = new DatosMiembroFamiliar(Parentesco.Hijo, 10, Ocupacion.Estudiante, 100.50m);
        var segundo = new DatosMiembroFamiliar(Parentesco.Hijo, 10, Ocupacion.Estudiante, 100.50m);
        var tercero = new DatosMiembroFamiliar(Parentesco.Hijo, 11, Ocupacion.Estudiante, 100.50m);

        Assert.Equal(primero, segundo);
        Assert.NotEqual(primero, tercero);
    }

    [Fact]
    public void Debe_AceptarLosLimitesDelRangoDeEdad_Cuando_LaEdadEsValida()
    {
        var recienNacido = new DatosMiembroFamiliar(
            Parentesco.Hijo,
            DatosMiembroFamiliar.EdadMinima,
            Ocupacion.NoTrabaja,
            0m);

        var adultoMayor = new DatosMiembroFamiliar(
            Parentesco.Otro,
            DatosMiembroFamiliar.EdadMaxima,
            Ocupacion.Jubilado,
            500m);

        Assert.Equal(DatosMiembroFamiliar.EdadMinima, recienNacido.Edad);
        Assert.Equal(DatosMiembroFamiliar.EdadMaxima, adultoMayor.Edad);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(121)]
    public void Debe_RechazarLaEdad_Cuando_EstaFueraDelRango(int edad)
    {
        var excepcion = Assert.Throws<MiembroFamiliarInvalidoException>(
            () => new DatosMiembroFamiliar(Parentesco.Hijo, edad, Ocupacion.Estudiante, 0m));

        Assert.Equal(MiembroFamiliarInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarElParentesco_Cuando_NoPerteneceAlCatalogo()
    {
        var excepcion = Assert.Throws<CatalogoInvalidoException>(
            () => new DatosMiembroFamiliar((Parentesco)99, 30, Ocupacion.Otro, 0m));

        Assert.Equal(CatalogoInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarLaOcupacion_Cuando_NoPerteneceAlCatalogo()
    {
        var excepcion = Assert.Throws<CatalogoInvalidoException>(
            () => new DatosMiembroFamiliar(Parentesco.Hermano, 30, (Ocupacion)99, 0m));

        Assert.Equal(CatalogoInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarElIngreso_Cuando_EsNegativo()
    {
        var excepcion = Assert.Throws<MiembroFamiliarInvalidoException>(
            () => new DatosMiembroFamiliar(Parentesco.Pareja, 30, Ocupacion.Empleado, -0.01m));

        Assert.Equal(MiembroFamiliarInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarElIngreso_Cuando_TieneMasDeDosDecimales()
    {
        var excepcion = Assert.Throws<MiembroFamiliarInvalidoException>(
            () => new DatosMiembroFamiliar(Parentesco.Pareja, 30, Ocupacion.Empleado, 100.001m));

        Assert.Equal(MiembroFamiliarInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }
}
