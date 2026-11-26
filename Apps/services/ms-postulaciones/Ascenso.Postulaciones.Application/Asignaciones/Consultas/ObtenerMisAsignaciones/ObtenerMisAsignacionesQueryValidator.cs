using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerMisAsignaciones;

public sealed class ObtenerMisAsignacionesQueryValidator
    : AbstractValidator<ObtenerMisAsignacionesQuery>
{
    public ObtenerMisAsignacionesQueryValidator()
    {
        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.Tamano)
            .InclusiveBetween(
                ObtenerMisAsignacionesQuery.TamanoMinimo,
                ObtenerMisAsignacionesQuery.TamanoMaximo)
            .WithMessage(
                $"El tamaño de página debe estar entre {ObtenerMisAsignacionesQuery.TamanoMinimo} y {ObtenerMisAsignacionesQuery.TamanoMaximo}.");

        RuleFor(x => x.Estado)
            .IsInEnum()
            .WithMessage("El estado de asignación indicado no es válido.")
            .When(x => x.Estado.HasValue);
    }
}
