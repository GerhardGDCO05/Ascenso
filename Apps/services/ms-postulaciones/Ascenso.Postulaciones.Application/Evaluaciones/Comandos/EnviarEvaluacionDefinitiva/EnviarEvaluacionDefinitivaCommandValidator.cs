using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Comandos.EnviarEvaluacionDefinitiva;

public sealed class EnviarEvaluacionDefinitivaCommandValidator
    : AbstractValidator<EnviarEvaluacionDefinitivaCommand>
{
    public EnviarEvaluacionDefinitivaCommandValidator()
    {
        RuleFor(x => x.AsignacionEvaluacionId)
            .NotEmpty()
            .WithMessage("El identificador de la asignación de evaluación es obligatorio.");
    }
}
