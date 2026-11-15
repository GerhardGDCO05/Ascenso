using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;

/// <summary>
/// Valida los datos de entrada de la emisión antes de invocar dependencias.
/// </summary>
public sealed class EmitirOfertaBecaValidator : AbstractValidator<EmitirOfertaBecaCommand>
{
    public EmitirOfertaBecaValidator()
    {
        RuleFor(comando => comando.PostulacionId)
            .NotEmpty()
            .WithMessage("La postulación es obligatoria.");
    }
}
