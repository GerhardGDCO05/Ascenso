using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMisEvaluaciones;

public sealed class ObtenerMisEvaluacionesQueryValidator
    : AbstractValidator<ObtenerMisEvaluacionesQuery>
{
    public ObtenerMisEvaluacionesQueryValidator()
    {
        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.Tamano)
            .InclusiveBetween(
                ObtenerMisEvaluacionesQuery.TamanoMinimo,
                ObtenerMisEvaluacionesQuery.TamanoMaximo)
            .WithMessage(
                $"El tamaño de página debe estar entre {ObtenerMisEvaluacionesQuery.TamanoMinimo} y {ObtenerMisEvaluacionesQuery.TamanoMaximo}.");

        RuleFor(x => x.Estado)
            .IsInEnum()
            .WithMessage("El estado de evaluación indicado no es válido.")
            .When(x => x.Estado.HasValue);
    }
}
