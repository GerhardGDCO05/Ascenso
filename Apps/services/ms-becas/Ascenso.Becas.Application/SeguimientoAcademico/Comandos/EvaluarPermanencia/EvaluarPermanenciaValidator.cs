using FluentValidation;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.EvaluarPermanencia;

public sealed class EvaluarPermanenciaValidator : AbstractValidator<EvaluarPermanenciaCommand>
{
    public EvaluarPermanenciaValidator()
    {
        RuleFor(comando => comando.ReporteAcademicoId)
            .NotEmpty().WithMessage("El reporte académico es obligatorio.");
    }
}
