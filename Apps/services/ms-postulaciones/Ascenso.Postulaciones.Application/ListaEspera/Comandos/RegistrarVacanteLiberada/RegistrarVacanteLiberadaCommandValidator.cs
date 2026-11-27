using Ascenso.Postulaciones.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.ListaEspera.Comandos.RegistrarVacanteLiberada;

public sealed class RegistrarVacanteLiberadaCommandValidator
    : AbstractValidator<RegistrarVacanteLiberadaCommand>
{
    public RegistrarVacanteLiberadaCommandValidator()
    {
        RuleFor(x => x.ConvocatoriaId)
            .NotEmpty()
            .WithErrorCode(ListaEsperaInvalidaException.CodigoError)
            .WithMessage("El identificador de la convocatoria es obligatorio.");

        RuleFor(x => x.MotivoVacante)
            .Must(motivo => motivo!.Trim().Length <= RegistrarVacanteLiberadaCommand.LongitudMaximaMotivoVacante)
            .When(x => !string.IsNullOrWhiteSpace(x.MotivoVacante))
            .WithErrorCode(ListaEsperaInvalidaException.CodigoError)
            .WithMessage(
                $"El motivo de la vacante liberada no puede superar los {RegistrarVacanteLiberadaCommand.LongitudMaximaMotivoVacante} caracteres.");
    }
}
