using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.CerrarPeriodoPrograma;

public sealed class CerrarPeriodoProgramaCommandValidator : AbstractValidator<CerrarPeriodoProgramaCommand>
{
    public CerrarPeriodoProgramaCommandValidator()
    {
        RuleFor(x => x.ProgramaBecaId)
            .NotEmpty()
            .WithErrorCode("PROGRAMA_ID_REQUERIDO")
            .WithMessage("El identificador del programa de becas es obligatorio.");

        RuleFor(x => x.PeriodoAcademicoId)
            .NotEmpty()
            .MaximumLength(40)
            .WithErrorCode("PERIODO_ACADEMICO_REQUERIDO")
            .WithMessage("El período académico a cerrar es obligatorio.");

        RuleFor(x => x.Observaciones)
            .NotEmpty()
            .MaximumLength(1000)
            .WithErrorCode("OBSERVACIONES_CIERRE_REQUERIDAS")
            .WithMessage("Las observaciones del cierre de período son obligatorias.");

        RuleFor(x => x.UsuarioModificacionId)
            .NotEmpty()
            .WithErrorCode("USUARIO_MODIFICACION_REQUERIDO")
            .WithMessage("El identificador del usuario de modificación es obligatorio.");
    }
}
