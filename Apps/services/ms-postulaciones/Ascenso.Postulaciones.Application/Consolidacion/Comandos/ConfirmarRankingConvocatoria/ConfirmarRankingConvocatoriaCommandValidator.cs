using Ascenso.Postulaciones.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;

public sealed class ConfirmarRankingConvocatoriaCommandValidator
    : AbstractValidator<ConfirmarRankingConvocatoriaCommand>
{
    public ConfirmarRankingConvocatoriaCommandValidator()
    {
        RuleFor(x => x.ConvocatoriaId)
            .NotEmpty()
            .WithErrorCode(RankingInvalidoException.CodigoError)
            .WithMessage("El identificador de la convocatoria es obligatorio.");
    }
}
