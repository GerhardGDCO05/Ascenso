using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using FluentAssertions;

namespace Ascenso.Programas.Domain.Tests.Donantes;

public sealed class ContactoDonanteTests
{
    [Fact]
    public void Debe_CrearContactoDonanteYRecortarEspacios_Cuando_DatosSonValidos()
    {
        var contacto = new ContactoDonante(
            "  María Fernanda Pérez  ",
            "  Directora de Responsabilidad Social  ",
            "  contacto@empresa.org  ",
            "  +58 212 5550101  ");

        contacto.NombreRepresentante.Should().Be("María Fernanda Pérez");
        contacto.Cargo.Should().Be("Directora de Responsabilidad Social");
        contacto.EmailContacto.Should().Be("contacto@empresa.org");
        contacto.TelefonoContacto.Should().Be("+58 212 5550101");
    }

    [Theory]
    [InlineData("", "Gerente", "correo@empresa.org", "+582125550101")]
    [InlineData("Juan", "", "correo@empresa.org", "+582125550101")]
    [InlineData("Juan", "Gerente", "correo-invalido", "+582125550101")]
    [InlineData("Juan", "Gerente", "correo@empresa.org", "")]
    public void Debe_LanzarArgumentException_Cuando_AlgunCampoDelContactoEsInvalido(
        string nombreRepresentante,
        string cargo,
        string emailContacto,
        string telefonoContacto)
    {
        var accion = () => new ContactoDonante(nombreRepresentante, cargo, emailContacto, telefonoContacto);

        accion.Should().Throw<ArgumentException>();
    }
}

