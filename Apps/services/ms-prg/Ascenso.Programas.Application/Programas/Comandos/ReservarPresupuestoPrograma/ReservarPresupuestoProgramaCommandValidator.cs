using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.ReservarPresupuestoPrograma;

public sealed class ReservarPresupuestoProgramaCommandValidator : AbstractValidator<ReservarPresupuestoProgramaCommand>
{
    public ReservarPresupuestoProgramaCommandValidator()
    {
        RuleFor(x => x.ProgramaBecaId)
            .NotEmpty()
            .WithErrorCode("PROGRAMA_ID_REQUERIDO")
            .WithMessage("El identificador del programa de becas es obligatorio.");

        RuleFor(x => x.ReferenciaExternaId)
            .NotEmpty()
            .WithErrorCode("REFERENCIA_EXTERNA_REQUERIDA")
            .WithMessage("La referencia externa de la reserva es obligatoria.");

        RuleFor(x => x.PeriodoAcademicoId)
            .NotEmpty()
            .MaximumLength(40)
            .WithErrorCode("PERIODO_ACADEMICO_REQUERIDO")
            .WithMessage("El período académico de la reserva es obligatorio.");

        RuleFor(x => x.Monto)
            .GreaterThan(0.00m)
            .WithErrorCode("MONTO_RESERVA_INVALIDO")
            .WithMessage("El monto a reservar debe ser mayor que cero.");

        RuleFor(x => x.Descripcion)
            .NotEmpty()
            .MaximumLength(500)
            .WithErrorCode("DESCRIPCION_RESERVA_REQUERIDA")
            .WithMessage("La descripción de la reserva es obligatoria.");

        RuleFor(x => x.UsuarioAutorId)
            .NotEmpty()
            .WithErrorCode("USUARIO_AUTOR_REQUERIDO")
            .WithMessage("El identificador del usuario autor es obligatorio.");
    }
}
