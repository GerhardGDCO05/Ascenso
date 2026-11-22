using Ascenso.Postulaciones.Application.Rubricas.Comandos.CrearRubrica;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Rubricas.Comandos;

public sealed class CrearRubricaCommandValidatorTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000201");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000202");

    private readonly CrearRubricaCommandValidator _validator = new();

    [Fact]
    public void Debe_AceptarComando_Cuando_LosDatosSonValidos()
    {
        var comando = new CrearRubricaCommand(
            _programaBecaId,
            _convocatoriaId,
            new string('R', 200));

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ProgramaBecaIdEstaVacio()
    {
        var comando = new CrearRubricaCommand(
            Guid.Empty,
            _convocatoriaId,
            "Rúbrica 2026-II");

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.ProgramaBecaId));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ConvocatoriaIdEstaVacio()
    {
        var comando = new CrearRubricaCommand(
            _programaBecaId,
            Guid.Empty,
            "Rúbrica 2026-II");

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.ConvocatoriaId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarComando_Cuando_ElNombreEstaVacio(string nombreInvalido)
    {
        var comando = new CrearRubricaCommand(
            _programaBecaId,
            _convocatoriaId,
            nombreInvalido);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Nombre));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ElNombreSupera200Caracteres()
    {
        var comando = new CrearRubricaCommand(
            _programaBecaId,
            _convocatoriaId,
            new string('R', 201));

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Nombre));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ElNombreConEspaciosSupera200CaracteresTrasRecortar()
    {
        var comando = new CrearRubricaCommand(
            _programaBecaId,
            _convocatoriaId,
            $"  {new string('R', 201)}  ");

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Nombre));
    }
}
