using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AsignarEvaluadoresPostulacion;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Comandos;

public sealed class AsignarEvaluadoresPostulacionCommandValidatorTests
{
    private static readonly DateTimeOffset _fechaLimite =
        new(2026, 12, 10, 23, 59, 0, TimeSpan.Zero);

    private readonly AsignarEvaluadoresPostulacionCommandValidator _validator = new();

    [Fact]
    public void Debe_SerValido_Cuando_ModoManualTieneUnOMasEvaluadoresValidos()
    {
        var comandoUno = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fechaLimite,
            UsuariosEvaluadoresIds: [Guid.NewGuid()]);

        var comandoDos = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fechaLimite,
            UsuariosEvaluadoresIds: [Guid.NewGuid(), Guid.NewGuid()]);

        _validator.Validate(comandoUno).IsValid.Should().BeTrue();
        _validator.Validate(comandoDos).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_SerValido_Cuando_ModoAutomaticoTieneCantidadMayorOIgualADos()
    {
        var comando = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fechaLimite,
            UsuariosEvaluadoresIds: null,
            CantidadEvaluadores: 2);

        _validator.Validate(comando).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_Rechazar_Cuando_ComiteIdOPostulacionIdEstanVacios()
    {
        var comandoSinComite = new AsignarEvaluadoresPostulacionCommand(
            Guid.Empty,
            Guid.NewGuid(),
            _fechaLimite,
            CantidadEvaluadores: 2);

        var comandoSinPostulacion = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.Empty,
            _fechaLimite,
            CantidadEvaluadores: 2);

        _validator.Validate(comandoSinComite).IsValid.Should().BeFalse();
        _validator.Validate(comandoSinPostulacion).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Debe_Rechazar_Cuando_FechaLimiteEvaluacionEsDefault()
    {
        var comando = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            default,
            CantidadEvaluadores: 2);

        _validator.Validate(comando).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Debe_Rechazar_Cuando_NoSeIndicaNiListaNiCantidad()
    {
        var comandoNull = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fechaLimite,
            UsuariosEvaluadoresIds: null,
            CantidadEvaluadores: null);

        var comandoListaVacia = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fechaLimite,
            UsuariosEvaluadoresIds: [],
            CantidadEvaluadores: null);

        _validator.Validate(comandoNull).IsValid.Should().BeFalse();
        _validator.Validate(comandoListaVacia).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Debe_Rechazar_Cuando_SeIndicanAmbosModosSimultaneamente()
    {
        var comando = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fechaLimite,
            UsuariosEvaluadoresIds: [Guid.NewGuid(), Guid.NewGuid()],
            CantidadEvaluadores: 2);

        _validator.Validate(comando).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Debe_Rechazar_Cuando_CantidadEvaluadoresEsMenorQueDos(int cantidadInvalida)
    {
        var comando = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fechaLimite,
            UsuariosEvaluadoresIds: null,
            CantidadEvaluadores: cantidadInvalida);

        _validator.Validate(comando).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Debe_Rechazar_Cuando_AlgunIdentificadorDeEvaluadorEstaVacio()
    {
        var comando = new AsignarEvaluadoresPostulacionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fechaLimite,
            UsuariosEvaluadoresIds: [Guid.NewGuid(), Guid.Empty]);

        _validator.Validate(comando).IsValid.Should().BeFalse();
    }
}
