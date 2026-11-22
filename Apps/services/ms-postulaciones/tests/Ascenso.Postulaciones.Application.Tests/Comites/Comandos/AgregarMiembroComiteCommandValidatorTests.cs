using Ascenso.Postulaciones.Application.Comites.Comandos.AgregarMiembroComite;
using Ascenso.Postulaciones.Domain.Enums;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Comites.Comandos;

public sealed class AgregarMiembroComiteCommandValidatorTests
{
    private static readonly Guid _comiteId = Guid.Parse("01966000-0000-7000-8000-000000000621");
    private static readonly Guid _evaluadorId = Guid.Parse("01966000-0000-7000-8000-000000000622");

    private readonly AgregarMiembroComiteCommandValidator _validator = new();

    [Theory]
    [InlineData(TipoEvaluador.Interno)]
    [InlineData(TipoEvaluador.Externo)]
    public void Debe_AceptarComando_Cuando_LosDatosSonValidos(TipoEvaluador tipo)
    {
        var comando = new AgregarMiembroComiteCommand(_comiteId, _evaluadorId, tipo);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ComiteIdEstaVacio()
    {
        var comando = new AgregarMiembroComiteCommand(Guid.Empty, _evaluadorId, TipoEvaluador.Interno);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.ComiteId));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_UsuarioEvaluadorIdEstaVacio()
    {
        var comando = new AgregarMiembroComiteCommand(_comiteId, Guid.Empty, TipoEvaluador.Externo);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.UsuarioEvaluadorId));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_TipoNoEstaDefinido()
    {
        var comando = new AgregarMiembroComiteCommand(_comiteId, _evaluadorId, (TipoEvaluador)999);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Tipo));
    }
}
