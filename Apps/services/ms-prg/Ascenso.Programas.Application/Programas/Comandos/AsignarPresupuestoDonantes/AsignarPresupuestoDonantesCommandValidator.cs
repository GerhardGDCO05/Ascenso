using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.AsignarPresupuestoDonantes;

public sealed class AsignarPresupuestoDonantesCommandValidator : AbstractValidator<AsignarPresupuestoDonantesCommand>
{
    public AsignarPresupuestoDonantesCommandValidator()
    {
        RuleFor(cmd => cmd.ProgramaBecaId)
            .NotEqual(Guid.Empty);

        RuleFor(cmd => cmd.AutorizadoPorUsuarioId)
            .NotEqual(Guid.Empty);

        RuleFor(cmd => cmd.Asignaciones)
            .NotEmpty()
            .WithMessage("Debe especificar al menos una asignación de presupuesto de donante.");

        RuleForEach(cmd => cmd.Asignaciones).ChildRules(item =>
        {
            item.RuleFor(x => x.DonanteId).NotEqual(Guid.Empty);
            item.RuleFor(x => x.PeriodoAcademicoId).NotEmpty().MaximumLength(30);
            item.RuleFor(x => x.MontoAsignado).GreaterThan(0.00m);
        });
    }
}
