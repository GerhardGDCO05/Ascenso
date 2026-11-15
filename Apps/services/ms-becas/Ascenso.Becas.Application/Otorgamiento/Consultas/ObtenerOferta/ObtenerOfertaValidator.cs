using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerOferta;

/// <summary>
/// Valida el identificador de la oferta consultada.
/// </summary>
public sealed class ObtenerOfertaValidator : AbstractValidator<ObtenerOfertaQuery>
{
    public ObtenerOfertaValidator()
    {
        RuleFor(consulta => consulta.OfertaId)
            .NotEmpty()
            .WithMessage("La oferta es obligatoria.");
    }
}
