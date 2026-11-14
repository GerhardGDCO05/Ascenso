using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.LiberarReservaPresupuesto;

public sealed class LiberarReservaPresupuestoCommandValidator : AbstractValidator<LiberarReservaPresupuestoCommand>
{
    public LiberarReservaPresupuestoCommandValidator()
    {
        RuleFor(x => x.ProgramaBecaId)
            .NotEmpty()
            .WithErrorCode("PROGRAMA_ID_REQUERIDO")
            .WithMessage("El identificador del programa de becas es obligatorio.");

        RuleFor(x => x.ReferenciaExternaId)
            .NotEmpty()
            .WithErrorCode("REFERENCIA_EXTERNA_REQUERIDA")
            .WithMessage("La referencia externa de la reserva es obligatoria.");

        RuleFor(x => x.Motivo)
            .NotEmpty()
            .MaximumLength(500)
            .WithErrorCode("MOTIVO_LIBERACION_REQUERIDO")
            .WithMessage("El motivo de liberación de la reserva es obligatorio.");

        RuleFor(x => x.UsuarioModificacionId)
            .NotEmpty()
            .WithErrorCode("USUARIO_MODIFICACION_REQUERIDO")
            .WithMessage("El identificador del usuario de modificación es obligatorio.");
    }
}
