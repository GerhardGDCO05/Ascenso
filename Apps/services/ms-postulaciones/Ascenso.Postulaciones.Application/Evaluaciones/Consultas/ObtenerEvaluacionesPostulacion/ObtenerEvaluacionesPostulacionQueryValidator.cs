using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesPostulacion;

public sealed class ObtenerEvaluacionesPostulacionQueryValidator
    : AbstractValidator<ObtenerEvaluacionesPostulacionQuery>
{
    public ObtenerEvaluacionesPostulacionQueryValidator()
    {
        RuleFor(x => x.PostulacionId)
            .NotEmpty()
            .WithMessage("El identificador de la postulación es obligatorio.");
    }
}
