using Ascenso.Becas.Domain.Enums;
using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.CicloVidaBeca;

public sealed class SuspenderBecaValidator : AbstractValidator<SuspenderBecaCommand>
{
    public SuspenderBecaValidator()
    {
        RuleFor(item => item.BecaId).NotEmpty();
        RuleFor(item => item.Tipo).Equal(TipoSuspensionBeca.Manual);
        RuleFor(item => item.Motivo).NotEmpty().MinimumLength(10);
        RuleFor(item => item.RequisitosParaReanudar).NotEmpty();
        RuleFor(item => item.FechaInicio).NotEmpty();
        RuleFor(item => item.FechaRevision)
            .GreaterThanOrEqualTo(item => item.FechaInicio)
            .When(item => item.FechaRevision.HasValue);
        RuleFor(item => item).Must(item => item.EsIndefinida != item.FechaRevision.HasValue)
            .WithMessage("La suspensión requiere una fecha de revisión o una marca indefinida.");
        RuleFor(item => item.Justificacion).NotEmpty().MinimumLength(10);
    }
}

public sealed class ReanudarBecaValidator : AbstractValidator<ReanudarBecaCommand>
{
    public ReanudarBecaValidator()
    {
        RuleFor(item => item.BecaId).NotEmpty();
        RuleFor(item => item.Justificacion).NotEmpty().MinimumLength(10);
    }
}

public sealed class RevocarBecaDefinitivamenteValidator
    : AbstractValidator<RevocarBecaDefinitivamenteCommand>
{
    public RevocarBecaDefinitivamenteValidator()
    {
        RuleFor(item => item.BecaId).NotEmpty();
        RuleFor(item => item.Justificacion).NotEmpty().MinimumLength(10);
        RuleFor(item => item.CodigoOtp).NotEmpty();
    }
}
