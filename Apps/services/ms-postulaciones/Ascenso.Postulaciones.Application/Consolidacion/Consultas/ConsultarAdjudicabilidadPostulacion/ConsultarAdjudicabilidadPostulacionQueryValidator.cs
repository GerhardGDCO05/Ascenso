using Ascenso.Postulaciones.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Consultas.ConsultarAdjudicabilidadPostulacion;

public sealed class ConsultarAdjudicabilidadPostulacionQueryValidator
    : AbstractValidator<ConsultarAdjudicabilidadPostulacionQuery>
{
    public ConsultarAdjudicabilidadPostulacionQueryValidator()
    {
        RuleFor(x => x.PostulacionId)
            .NotEmpty()
            .WithErrorCode(RankingInvalidoException.CodigoError)
            .WithMessage("El identificador de la postulación es obligatorio.");
    }
}
