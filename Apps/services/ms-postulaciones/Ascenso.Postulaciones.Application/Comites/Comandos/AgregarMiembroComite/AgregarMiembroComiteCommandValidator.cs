using FluentValidation;

namespace Ascenso.Postulaciones.Application.Comites.Comandos.AgregarMiembroComite;

public sealed class AgregarMiembroComiteCommandValidator : AbstractValidator<AgregarMiembroComiteCommand>
{
    public AgregarMiembroComiteCommandValidator()
    {
        RuleFor(x => x.ComiteId)
            .NotEmpty()
            .WithMessage("El identificador del comité evaluador es obligatorio.");

        RuleFor(x => x.UsuarioEvaluadorId)
            .NotEmpty()
            .WithMessage("El identificador del usuario evaluador es obligatorio.");

        RuleFor(x => x.Tipo)
            .IsInEnum()
            .WithMessage("El tipo de evaluador no es válido.");
    }
}
