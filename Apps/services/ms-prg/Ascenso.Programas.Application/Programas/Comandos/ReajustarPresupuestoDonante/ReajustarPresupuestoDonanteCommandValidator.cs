using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.ReajustarPresupuestoDonante;

public sealed class ReajustarPresupuestoDonanteCommandValidator : AbstractValidator<ReajustarPresupuestoDonanteCommand>
{
    public ReajustarPresupuestoDonanteCommandValidator()
    {
        RuleFor(cmd => cmd.ProgramaBecaId)
            .NotEqual(Guid.Empty);

        RuleFor(cmd => cmd.AsignacionId)
            .NotEqual(Guid.Empty);

        RuleFor(cmd => cmd.MontoAReducir)
            .GreaterThan(0.00m);

        RuleFor(cmd => cmd.UsuarioModificacionId)
            .NotEqual(Guid.Empty);
    }
}
