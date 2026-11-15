using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;

/// <summary>
/// Valida los datos de entrada de la aceptación.
/// </summary>
public sealed class AceptarOfertaBecaValidator : AbstractValidator<AceptarOfertaBecaCommand>
{
    public AceptarOfertaBecaValidator()
    {
        RuleFor(comando => comando.OfertaId)
            .NotEmpty()
            .WithMessage("La oferta es obligatoria.");
    }
}
