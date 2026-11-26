using FluentValidation;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.ConfirmarEvaluacionPermanencia;

public sealed class ConfirmarEvaluacionPermanenciaValidator
    : AbstractValidator<ConfirmarEvaluacionPermanenciaCommand>
{
    public ConfirmarEvaluacionPermanenciaValidator()
    {
        RuleFor(comando => comando.ReporteAcademicoId)
            .NotEmpty().WithMessage("El reporte académico es obligatorio.");
        RuleFor(comando => comando.MotivoDecision)
            .NotEmpty().MaximumLength(1000);
        RuleFor(comando => comando.SiguientePasoBecario)
            .NotEmpty().MaximumLength(1000);
    }
}
