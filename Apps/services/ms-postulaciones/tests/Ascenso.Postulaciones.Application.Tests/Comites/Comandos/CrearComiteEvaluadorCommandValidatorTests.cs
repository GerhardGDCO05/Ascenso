using Ascenso.Postulaciones.Application.Comites.Comandos.CrearComiteEvaluador;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Comites.Comandos;

public sealed class CrearComiteEvaluadorCommandValidatorTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000601");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000602");
    private static readonly Guid _jefaturaUsuarioId = Guid.Parse("01966000-0000-7000-8000-000000000603");

    private readonly CrearComiteEvaluadorCommandValidator _validator = new();

    [Fact]
    public void Debe_AceptarComando_Cuando_LosDatosSonValidos()
    {
        var comando = new CrearComiteEvaluadorCommand(
            _programaBecaId,
            _convocatoriaId,
            new string('C', 200),
            _jefaturaUsuarioId);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ProgramaBecaIdEstaVacio()
    {
        var comando = new CrearComiteEvaluadorCommand(
            Guid.Empty,
            _convocatoriaId,
            "Comité 2026-II",
            _jefaturaUsuarioId);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.ProgramaBecaId));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ConvocatoriaIdEstaVacio()
    {
        var comando = new CrearComiteEvaluadorCommand(
            _programaBecaId,
            Guid.Empty,
            "Comité 2026-II",
            _jefaturaUsuarioId);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.ConvocatoriaId));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_JefaturaUsuarioIdEstaVacio()
    {
        var comando = new CrearComiteEvaluadorCommand(
            _programaBecaId,
            _convocatoriaId,
            "Comité 2026-II",
            Guid.Empty);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.JefaturaUsuarioId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarComando_Cuando_ElNombreEstaVacio(string nombreInvalido)
    {
        var comando = new CrearComiteEvaluadorCommand(
            _programaBecaId,
            _convocatoriaId,
            nombreInvalido,
            _jefaturaUsuarioId);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Nombre));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ElNombreSupera200Caracteres()
    {
        var comando = new CrearComiteEvaluadorCommand(
            _programaBecaId,
            _convocatoriaId,
            new string('C', 201),
            _jefaturaUsuarioId);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Nombre));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ElNombreConEspaciosSupera200CaracteresTrasRecortar()
    {
        var comando = new CrearComiteEvaluadorCommand(
            _programaBecaId,
            _convocatoriaId,
            $"  {new string('C', 201)}  ",
            _jefaturaUsuarioId);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Nombre));
    }
}
