using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerResultadoConsolidadoPostulacion;

public sealed class ObtenerResultadoConsolidadoPostulacionQueryValidator
    : AbstractValidator<ObtenerResultadoConsolidadoPostulacionQuery>
{
    public ObtenerResultadoConsolidadoPostulacionQueryValidator()
    {
        RuleFor(x => x.PostulacionId)
            .NotEmpty()
            .WithMessage("El identificador de la postulación es obligatorio.");
    }
}
