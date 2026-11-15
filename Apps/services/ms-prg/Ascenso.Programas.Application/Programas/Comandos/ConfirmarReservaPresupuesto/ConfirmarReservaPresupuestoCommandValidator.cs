using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.ConfirmarReservaPresupuesto;

public sealed class ConfirmarReservaPresupuestoCommandValidator : AbstractValidator<ConfirmarReservaPresupuestoCommand>
{
    public ConfirmarReservaPresupuestoCommandValidator()
    {
        RuleFor(x => x.ProgramaBecaId)
            .NotEmpty()
            .WithErrorCode("PROGRAMA_ID_REQUERIDO")
            .WithMessage("El identificador del programa de becas es obligatorio.");

        RuleFor(x => x.ReferenciaExternaId)
            .NotEmpty()
            .WithErrorCode("REFERENCIA_EXTERNA_REQUERIDA")
            .WithMessage("La referencia externa de la reserva es obligatoria.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Descripcion))
            .WithErrorCode("DESCRIPCION_CONFIRMACION_INVALIDA")
            .WithMessage("La descripción de confirmación no puede superar los 500 caracteres.");

        RuleFor(x => x.UsuarioModificacionId)
            .NotEmpty()
            .WithErrorCode("USUARIO_MODIFICACION_REQUERIDO")
            .WithMessage("El identificador del usuario de modificación es obligatorio.");
    }
}
