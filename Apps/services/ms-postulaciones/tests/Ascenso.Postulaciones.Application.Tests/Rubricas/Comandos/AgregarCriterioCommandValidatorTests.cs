using Ascenso.Postulaciones.Application.Rubricas.Comandos.AgregarCriterio;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Rubricas.Comandos;

public sealed class AgregarCriterioCommandValidatorTests
{
    private static readonly Guid _rubricaId = Guid.Parse("01966000-0000-7000-8000-000000000301");

    private readonly AgregarCriterioCommandValidator _validator = new();

    [Fact]
    public void Debe_AceptarComando_Cuando_LosDatosSonValidosEnLosLimites()
    {
        var comando = CrearComandoValido(
            nombre: new string('N', 200),
            descripcion: new string('D', 1000),
            pesoPorcentual: 100.00m,
            puntajeMinimo: 0.00m,
            puntajeMaximo: 1000.00m);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_RubricaIdEstaVacio()
    {
        var comando = CrearComandoValido() with { RubricaId = Guid.Empty };

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.RubricaId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarComando_Cuando_ElNombreEstaVacio(string nombreInvalido)
    {
        var comando = CrearComandoValido(nombre: nombreInvalido);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Nombre));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ElNombreSupera200Caracteres()
    {
        var comando = CrearComandoValido(nombre: new string('N', 201));

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Nombre));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarComando_Cuando_LaDescripcionEstaVacia(string descripcionInvalida)
    {
        var comando = CrearComandoValido(descripcion: descripcionInvalida);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Descripcion));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_LaDescripcionSupera1000Caracteres()
    {
        var comando = CrearComandoValido(descripcion: new string('D', 1001));

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.Descripcion));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    [InlineData(25.125)]
    public void Debe_RechazarComando_Cuando_ElPesoPorcentualEsInvalido(decimal pesoInvalido)
    {
        var comando = CrearComandoValido(pesoPorcentual: pesoInvalido);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == nameof(comando.PesoPorcentual));
    }

    [Theory]
    [InlineData(-0.01, 10.00)]
    [InlineData(0.00, 1000.01)]
    [InlineData(1000.01, 1000.02)]
    [InlineData(0.125, 10.00)]
    [InlineData(0.00, 10.125)]
    [InlineData(10.00, 10.00)]
    [InlineData(15.00, 10.00)]
    public void Debe_RechazarComando_Cuando_ElRangoDePuntajeEsInvalido(
        decimal puntajeMinimo,
        decimal puntajeMaximo)
    {
        var comando = CrearComandoValido(
            puntajeMinimo: puntajeMinimo,
            puntajeMaximo: puntajeMaximo);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error =>
            error.PropertyName == nameof(comando.PuntajeMinimo)
            || error.PropertyName == nameof(comando.PuntajeMaximo));
    }

    private static AgregarCriterioCommand CrearComandoValido(
        string nombre = "Rendimiento académico",
        string descripcion = "Promedio ponderado acumulado",
        decimal pesoPorcentual = 30.50m,
        decimal puntajeMinimo = 0.00m,
        decimal puntajeMaximo = 20.00m)
        => new(
            _rubricaId,
            nombre,
            descripcion,
            pesoPorcentual,
            puntajeMinimo,
            puntajeMaximo);
}
