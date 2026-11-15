using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.RechazarOfertaBeca;

/// <summary>
/// Valida los datos de entrada del rechazo.
/// </summary>
public sealed class RechazarOfertaBecaValidator : AbstractValidator<RechazarOfertaBecaCommand>
{
    public RechazarOfertaBecaValidator()
    {
        RuleFor(comando => comando.OfertaId)
            .NotEmpty()
            .WithMessage("La oferta es obligatoria.");

        RuleFor(comando => comando.Motivo)
            .MaximumLength(1000)
            .WithMessage("El motivo del rechazo no puede superar 1000 caracteres.");
    }
}
