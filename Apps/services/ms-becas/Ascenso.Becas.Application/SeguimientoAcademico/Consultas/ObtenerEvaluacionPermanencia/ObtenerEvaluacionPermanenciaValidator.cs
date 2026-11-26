using FluentValidation;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Consultas.ObtenerEvaluacionPermanencia;

public sealed class ObtenerEvaluacionPermanenciaValidator
    : AbstractValidator<ObtenerEvaluacionPermanenciaQuery>
{
    public ObtenerEvaluacionPermanenciaValidator()
    {
        RuleFor(consulta => consulta.EvaluacionPermanenciaId)
            .NotEmpty().WithMessage("La evaluación de permanencia es obligatoria.");
    }
}
