using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerAsignacionesPostulacion;

public sealed class ObtenerAsignacionesPostulacionQueryValidator
    : AbstractValidator<ObtenerAsignacionesPostulacionQuery>
{
    public ObtenerAsignacionesPostulacionQueryValidator()
    {
        RuleFor(x => x.PostulacionId)
            .NotEmpty()
            .WithMessage("El identificador de la postulación es obligatorio.");
    }
}
