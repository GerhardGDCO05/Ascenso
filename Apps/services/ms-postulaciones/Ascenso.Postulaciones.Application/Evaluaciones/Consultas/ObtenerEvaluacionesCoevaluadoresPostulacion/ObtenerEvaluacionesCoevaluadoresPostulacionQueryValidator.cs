using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesCoevaluadoresPostulacion;

public sealed class ObtenerEvaluacionesCoevaluadoresPostulacionQueryValidator
    : AbstractValidator<ObtenerEvaluacionesCoevaluadoresPostulacionQuery>
{
    public ObtenerEvaluacionesCoevaluadoresPostulacionQueryValidator()
    {
        RuleFor(x => x.PostulacionId)
            .NotEmpty()
            .WithErrorCode("EVALUACION_INVALIDA")
            .WithMessage("El identificador de la postulación es obligatorio.");
    }
}
