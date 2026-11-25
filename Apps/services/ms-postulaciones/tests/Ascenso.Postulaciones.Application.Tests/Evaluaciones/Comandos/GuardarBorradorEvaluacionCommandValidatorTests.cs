using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.Comandos;

public sealed class GuardarBorradorEvaluacionCommandValidatorTests
{
    private readonly GuardarBorradorEvaluacionCommandValidator _validator = new();

    [Fact]
    public void Debe_AceptarComandoValido_Cuando_TodosLosCamposCumplenLasReglas()
    {
        var comando = new GuardarBorradorEvaluacionCommand(
            Guid.NewGuid(),
            [
                new DatosCalificacionCriterioCommand(Guid.NewGuid(), 0.00m, "Comentario válido"),
                new DatosCalificacionCriterioCommand(Guid.NewGuid(), 1000.00m, null)
            ],
            "Comentarios generales");

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_Rechazar_Cuando_AsignacionIdEsVacio()
    {
        var comando = new GuardarBorradorEvaluacionCommand(
            Guid.Empty,
            [new DatosCalificacionCriterioCommand(Guid.NewGuid(), 10.00m, null)]);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(GuardarBorradorEvaluacionCommand.AsignacionEvaluacionId));
    }

    [Fact]
    public void Debe_Rechazar_Cuando_ListaDeCalificacionesEstaVacia()
    {
        var comando = new GuardarBorradorEvaluacionCommand(Guid.NewGuid(), []);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(GuardarBorradorEvaluacionCommand.Calificaciones));
    }

    [Fact]
    public void Debe_Rechazar_Cuando_HayCriteriosDuplicados()
    {
        var criterioId = Guid.NewGuid();
        var comando = new GuardarBorradorEvaluacionCommand(
            Guid.NewGuid(),
            [
                new DatosCalificacionCriterioCommand(criterioId, 10.00m, null),
                new DatosCalificacionCriterioCommand(criterioId, 12.00m, null)
            ]);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(GuardarBorradorEvaluacionCommand.Calificaciones));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1000.01)]
    [InlineData(10.123)]
    public void Debe_Rechazar_Cuando_PuntajeEstaFueraDeRangoTecnicoOTieneMasDeDosDecimales(decimal puntajeInvalido)
    {
        var comando = new GuardarBorradorEvaluacionCommand(
            Guid.NewGuid(),
            [new DatosCalificacionCriterioCommand(Guid.NewGuid(), puntajeInvalido, null)]);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Debe_Rechazar_Cuando_CriterioIdEsVacioOComentariosExcedenLongitudMaxima()
    {
        var comando = new GuardarBorradorEvaluacionCommand(
            Guid.NewGuid(),
            [
                new DatosCalificacionCriterioCommand(
                    Guid.Empty,
                    10.00m,
                    new string('a', DatosCalificacionCriterio.LongitudMaximaComentario + 1))
            ],
            new string('b', Evaluacion.LongitudMaximaComentariosGenerales + 1));

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}
