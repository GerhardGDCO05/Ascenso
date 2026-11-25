using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.EnviarEvaluacionDefinitiva;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.Comandos;

public sealed class EnviarEvaluacionDefinitivaCommandValidatorTests
{
    private readonly EnviarEvaluacionDefinitivaCommandValidator _validator = new();

    [Fact]
    public void Debe_AceptarComandoValido_Cuando_AsignacionIdEsNoVacio()
    {
        var resultado = _validator.Validate(new EnviarEvaluacionDefinitivaCommand(Guid.NewGuid()));

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_Rechazar_Cuando_AsignacionIdEsVacio()
    {
        var resultado = _validator.Validate(new EnviarEvaluacionDefinitivaCommand(Guid.Empty));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(EnviarEvaluacionDefinitivaCommand.AsignacionEvaluacionId));
    }
}
