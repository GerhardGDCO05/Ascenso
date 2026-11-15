using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Consultas.ConsultarBecasActivas;

/// <summary>
/// Exige el estudiante y el periodo de la consulta interna.
/// </summary>
public sealed class ConsultarBecasActivasValidator
    : AbstractValidator<ConsultarBecasActivasQuery>
{
    public ConsultarBecasActivasValidator()
    {
        RuleFor(consulta => consulta.PostulanteEstudianteId)
            .NotEmpty()
            .WithMessage("El estudiante es obligatorio.");

        RuleFor(consulta => consulta.PeriodoAcademicoId)
            .NotEmpty()
            .WithMessage("El periodo académico es obligatorio.");
    }
}
