using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.ExpirarOfertaBeca;

/// <summary>
/// Valida el identificador de la oferta a expirar.
/// </summary>
public sealed class ExpirarOfertaBecaValidator : AbstractValidator<ExpirarOfertaBecaCommand>
{
    public ExpirarOfertaBecaValidator()
    {
        RuleFor(comando => comando.OfertaId)
            .NotEmpty()
            .WithMessage("La oferta es obligatoria.");
    }
}
