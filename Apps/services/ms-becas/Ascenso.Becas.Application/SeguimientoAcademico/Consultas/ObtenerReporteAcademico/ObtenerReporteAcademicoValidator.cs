using FluentValidation;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Consultas.ObtenerReporteAcademico;

public sealed class ObtenerReporteAcademicoValidator
    : AbstractValidator<ObtenerReporteAcademicoQuery>
{
    public ObtenerReporteAcademicoValidator()
    {
        RuleFor(consulta => consulta.ReporteAcademicoId)
            .NotEmpty().WithMessage("El reporte académico es obligatorio.");
    }
}
