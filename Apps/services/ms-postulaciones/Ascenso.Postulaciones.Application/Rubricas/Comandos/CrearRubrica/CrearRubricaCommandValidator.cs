using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.CrearRubrica;

public sealed class CrearRubricaCommandValidator : AbstractValidator<CrearRubricaCommand>
{
    public CrearRubricaCommandValidator()
    {
        RuleFor(x => x.ProgramaBecaId)
            .NotEmpty()
            .WithMessage("El identificador del programa de beca es obligatorio.");

        RuleFor(x => x.ConvocatoriaId)
            .NotEmpty()
            .WithMessage("El identificador de la convocatoria es obligatorio.");

        RuleFor(x => x.Nombre)
            .NotEmpty()
            .WithMessage("El nombre de la rúbrica de evaluación es obligatorio.")
            .Must(nombre => string.IsNullOrWhiteSpace(nombre)
                || nombre.Trim().Length <= RubricaEvaluacion.LongitudMaximaNombre)
            .WithMessage($"El nombre de la rúbrica de evaluación no puede superar los {RubricaEvaluacion.LongitudMaximaNombre} caracteres.");
    }
}
