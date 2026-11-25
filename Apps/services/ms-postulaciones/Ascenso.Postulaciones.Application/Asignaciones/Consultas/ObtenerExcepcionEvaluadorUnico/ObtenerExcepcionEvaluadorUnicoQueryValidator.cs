using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerExcepcionEvaluadorUnico;

public sealed class ObtenerExcepcionEvaluadorUnicoQueryValidator
    : AbstractValidator<ObtenerExcepcionEvaluadorUnicoQuery>
{
    public ObtenerExcepcionEvaluadorUnicoQueryValidator()
    {
        RuleFor(x => x.PostulacionId)
            .NotEmpty()
            .WithErrorCode("VALIDACION_INVALIDA")
            .WithMessage("El identificador de la postulación es obligatorio.");
    }
}
