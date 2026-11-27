using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.ListaEspera.Comandos.PromoverCandidatoListaEspera;

public sealed class PromoverCandidatoListaEsperaCommandValidator
    : AbstractValidator<PromoverCandidatoListaEsperaCommand>
{
    public PromoverCandidatoListaEsperaCommandValidator()
    {
        RuleFor(x => x.ConvocatoriaId)
            .NotEmpty()
            .WithErrorCode(ListaEsperaInvalidaException.CodigoError)
            .WithMessage("El identificador de la convocatoria es obligatorio.");

        RuleFor(x => x.EntradaListaEsperaId)
            .Must(id => id != Guid.Empty)
            .When(x => x.EntradaListaEsperaId.HasValue)
            .WithErrorCode(ListaEsperaInvalidaException.CodigoError)
            .WithMessage("El identificador de la entrada de lista de espera no puede estar vacío.");

        RuleFor(x => x.MotivoPromocion)
            .Must(motivo => motivo!.Trim().Length <= EntradaListaEspera.LongitudMaximaMotivoPromocion)
            .When(x => !string.IsNullOrWhiteSpace(x.MotivoPromocion))
            .WithErrorCode(MotivoPromocionInvalidoException.CodigoError)
            .WithMessage(
                $"El motivo de promoción no puede superar los {EntradaListaEspera.LongitudMaximaMotivoPromocion} caracteres.");
    }
}
