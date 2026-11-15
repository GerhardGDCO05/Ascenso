using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.AnularOfertaBeca;

/// <summary>
/// Exige la justificación mínima de la anulación.
/// </summary>
public sealed class AnularOfertaBecaValidator : AbstractValidator<AnularOfertaBecaCommand>
{
    public AnularOfertaBecaValidator()
    {
        RuleFor(comando => comando.OfertaId)
            .NotEmpty()
            .WithMessage("La oferta es obligatoria.");

        RuleFor(comando => comando.Justificacion)
            .NotEmpty()
            .WithMessage("La justificación es obligatoria.")
            .MinimumLength(10)
            .WithMessage("La justificación debe tener al menos 10 caracteres.")
            .MaximumLength(1000)
            .WithMessage("La justificación no puede superar 1000 caracteres.");
    }
}
