using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;

namespace Ascenso.Postulaciones.Domain.Tests.Postulantes.ObjetosValor;

public sealed class DatosContactoTests
{
    [Fact]
    public void Debe_NormalizarCorreoYTelefono_Cuando_LosDatosSonValidos()
    {
        var contacto = new DatosContacto("  ana.perez@example.com  ", " +51 (999) 888-777 ");

        Assert.Equal("ana.perez@example.com", contacto.CorreoElectronico);
        Assert.Equal("+51999888777", contacto.Telefono);
    }

    [Fact]
    public void Debe_SerIgualPorValor_Cuando_LosDatosCoinciden()
    {
        var primero = new DatosContacto("ana@example.com", "999888777");
        var segundo = new DatosContacto("ana@example.com", "999888777");
        var tercero = new DatosContacto("otra@example.com", "999888777");

        Assert.Equal(primero, segundo);
        Assert.NotEqual(primero, tercero);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ana.example.com")]
    [InlineData("Ana Pérez <ana@example.com>")]
    [InlineData("ana @example.com")]
    public void Debe_RechazarElCorreo_Cuando_NoTieneUnFormatoValido(string correo)
    {
        Assert.Throws<DatosPersonalesInvalidosException>(
            () => new DatosContacto(correo, "999888777"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("1234567890123456")]
    [InlineData("999-888-ABC")]
    [InlineData("+51+999888777")]
    public void Debe_RechazarElTelefono_Cuando_NoEsValido(string telefono)
    {
        Assert.Throws<DatosPersonalesInvalidosException>(
            () => new DatosContacto("ana@example.com", telefono));
    }
}
