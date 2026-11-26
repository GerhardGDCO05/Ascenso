using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMiEvaluacionPorAsignacion;

public sealed class ObtenerMiEvaluacionPorAsignacionQueryValidator
    : AbstractValidator<ObtenerMiEvaluacionPorAsignacionQuery>
{
    public ObtenerMiEvaluacionPorAsignacionQueryValidator()
    {
        RuleFor(x => x.AsignacionEvaluacionId)
            .NotEmpty()
            .WithMessage("El identificador de la asignación de evaluación es obligatorio.");
    }
}
