using FluentValidation;

namespace Ascenso.Postulaciones.Application.Comites.Comandos.DesvincularMiembroComite;

public sealed class DesvincularMiembroComiteCommandValidator : AbstractValidator<DesvincularMiembroComiteCommand>
{
    public DesvincularMiembroComiteCommandValidator()
    {
        RuleFor(x => x.ComiteId)
            .NotEmpty()
            .WithMessage("El identificador del comité evaluador es obligatorio.");

        RuleFor(x => x.MiembroId)
            .NotEmpty()
            .WithMessage("El identificador del miembro del comité es obligatorio.");
    }
}
