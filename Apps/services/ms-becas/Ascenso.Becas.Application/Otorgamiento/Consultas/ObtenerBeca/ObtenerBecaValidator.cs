using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerBeca;

/// <summary>
/// Valida el identificador de la beca consultada.
/// </summary>
public sealed class ObtenerBecaValidator : AbstractValidator<ObtenerBecaQuery>
{
    public ObtenerBecaValidator()
    {
        RuleFor(consulta => consulta.BecaId)
            .NotEmpty()
            .WithMessage("La beca es obligatoria.");
    }
}
