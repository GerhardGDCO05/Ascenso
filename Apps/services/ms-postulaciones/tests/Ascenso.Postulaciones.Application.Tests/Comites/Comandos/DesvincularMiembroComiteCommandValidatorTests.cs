using Ascenso.Postulaciones.Application.Comites.Comandos.DesvincularMiembroComite;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Comites.Comandos;

public sealed class DesvincularMiembroComiteCommandValidatorTests
{
    private static readonly Guid _comiteId = Guid.Parse("01966000-0000-7000-8000-000000000641");
    private static readonly Guid _miembroId = Guid.Parse("01966000-0000-7000-8000-000000000642");

    private readonly DesvincularMiembroComiteCommandValidator _validator = new();

    [Fact]
    public void Debe_AceptarComando_Cuando_LosIdentificadoresSonValidos()
    {
        var comando = new DesvincularMiembroComiteCommand(_comiteId, _miembroId);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ComiteIdEstaVacio()
    {
        var comando = new DesvincularMiembroComiteCommand(Guid.Empty, _miembroId);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.ComiteId));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_MiembroIdEstaVacio()
    {
        var comando = new DesvincularMiembroComiteCommand(_comiteId, Guid.Empty);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.MiembroId));
    }
}
