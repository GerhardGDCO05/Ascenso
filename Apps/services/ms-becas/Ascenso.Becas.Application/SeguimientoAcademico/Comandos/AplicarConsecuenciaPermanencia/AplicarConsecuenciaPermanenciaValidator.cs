using FluentValidation;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.AplicarConsecuenciaPermanencia;

public sealed class AplicarConsecuenciaPermanenciaValidator
    : AbstractValidator<AplicarConsecuenciaPermanenciaCommand>
{
    public AplicarConsecuenciaPermanenciaValidator()
    {
        RuleFor(comando => comando.ReporteAcademicoId)
            .NotEmpty().WithMessage("El reporte académico es obligatorio.");
    }
}
