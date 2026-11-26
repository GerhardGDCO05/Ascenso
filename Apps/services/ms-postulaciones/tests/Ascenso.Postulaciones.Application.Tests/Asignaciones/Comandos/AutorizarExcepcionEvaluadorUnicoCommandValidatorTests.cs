using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AutorizarExcepcionEvaluadorUnico;
using Ascenso.Postulaciones.Domain.Excepciones;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Comandos;

public sealed class AutorizarExcepcionEvaluadorUnicoCommandValidatorTests
{
    private readonly AutorizarExcepcionEvaluadorUnicoCommandValidator _validator = new();

    [Fact]
    public void Debe_SerValido_Cuando_PostulacionYJustificacionCumplenLimites()
    {
        var comandoMinimo = new AutorizarExcepcionEvaluadorUnicoCommand(
            Guid.NewGuid(),
            $"  {new string('a', 10)}  ");

        var comandoMaximo = new AutorizarExcepcionEvaluadorUnicoCommand(
            Guid.NewGuid(),
            new string('b', 1000));

        _validator.Validate(comandoMinimo).IsValid.Should().BeTrue();
        _validator.Validate(comandoMaximo).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_Rechazar_Cuando_PostulacionIdEstaVacio()
    {
        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(
            Guid.Empty,
            "Justificación válida para autorizar evaluador único.");

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(AutorizarExcepcionEvaluadorUnicoCommand.PostulacionId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("          ")]
    [InlineData("123456789")]
    [InlineData("   123456789   ")]
    public void Debe_RechazarConCodigoJustificacionRequerida_Cuando_JustificacionEsInvalida(string? justificacion)
    {
        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(
            Guid.NewGuid(),
            justificacion!);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(AutorizarExcepcionEvaluadorUnicoCommand.Justificacion)
            && error.ErrorCode == JustificacionExcepcionInvalidaException.CodigoError);
    }

    [Fact]
    public void Debe_RechazarConCodigoJustificacionRequerida_Cuando_JustificacionSuperaMilCaracteres()
    {
        var comando = new AutorizarExcepcionEvaluadorUnicoCommand(
            Guid.NewGuid(),
            new string('x', 1001));

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(AutorizarExcepcionEvaluadorUnicoCommand.Justificacion)
            && error.ErrorCode == JustificacionExcepcionInvalidaException.CodigoError);
    }
}
