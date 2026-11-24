using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerCargaEvaluadoresComite;

public sealed class ObtenerCargaEvaluadoresComiteQueryValidator
    : AbstractValidator<ObtenerCargaEvaluadoresComiteQuery>
{
    public ObtenerCargaEvaluadoresComiteQueryValidator()
    {
        RuleFor(x => x.ComiteId)
            .NotEmpty()
            .WithMessage("El identificador del comité evaluador es obligatorio.");
    }
}
