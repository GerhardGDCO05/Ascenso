using Ascenso.Postulaciones.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConsolidarPostulacion;

public sealed class ConsolidarPostulacionCommandValidator
    : AbstractValidator<ConsolidarPostulacionCommand>
{
    public ConsolidarPostulacionCommandValidator()
    {
        RuleFor(x => x.PostulacionId)
            .NotEmpty()
            .WithErrorCode(ConsolidacionInvalidaException.CodigoError)
            .WithMessage("El identificador de la postulación es obligatorio.");
    }
}
