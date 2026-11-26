using Ascenso.Programas.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Programas.Application.Convocatorias.Comandos.EditarConvocatoriaBorrador;

public sealed class EditarConvocatoriaBorradorCommandValidator : AbstractValidator<EditarConvocatoriaBorradorCommand>
{
    public EditarConvocatoriaBorradorCommandValidator()
    {
        RuleFor(cmd => cmd.ConvocatoriaId)
            .NotEqual(Guid.Empty);

        RuleFor(cmd => cmd.PeriodoAcademicoId)
            .NotEmpty()
            .MaximumLength(36)
            .Must(periodoId => Guid.TryParse(periodoId, out var id) && id != Guid.Empty)
            .WithMessage("El identificador del periodo académico debe ser un UUID válido.");

        RuleFor(cmd => cmd.Nombre)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(cmd => cmd.CuposAsignados)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(CuposInsuficientesException.CodigoError);

        RuleFor(cmd => cmd.UsuarioModificacionId)
            .NotEqual(Guid.Empty);

        RuleFor(cmd => cmd.CriteriosElegibilidad)
            .NotNull()
            .WithErrorCode(CriteriosConvocatoriaInvalidosException.CodigoError);

        When(cmd => cmd.CriteriosElegibilidad is not null, () =>
        {
            RuleFor(cmd => cmd.CriteriosElegibilidad.PromedioMinimo)
                .InclusiveBetween(0.00m, 20.00m)
                .WithErrorCode(CriteriosConvocatoriaInvalidosException.CodigoError);

            RuleFor(cmd => cmd.CriteriosElegibilidad.EdadMinima)
                .InclusiveBetween(0, 120)
                .WithErrorCode(CriteriosConvocatoriaInvalidosException.CodigoError);

            RuleFor(cmd => cmd.CriteriosElegibilidad.EdadMaxima)
                .InclusiveBetween(0, 120)
                .GreaterThanOrEqualTo(cmd => cmd.CriteriosElegibilidad.EdadMinima)
                .WithErrorCode(CriteriosConvocatoriaInvalidosException.CodigoError);

            RuleFor(cmd => cmd.CriteriosElegibilidad.TopeIngresoFamiliar)
                .GreaterThanOrEqualTo(0.00m)
                .WithErrorCode(CriteriosConvocatoriaInvalidosException.CodigoError);

            RuleFor(cmd => cmd.CriteriosElegibilidad.UniversidadesPermitidasIds)
                .NotEmpty()
                .WithErrorCode(CriteriosConvocatoriaInvalidosException.CodigoError);

            RuleFor(cmd => cmd.CriteriosElegibilidad.CarrerasPermitidasIds)
                .NotEmpty()
                .WithErrorCode(CriteriosConvocatoriaInvalidosException.CodigoError);
        });
    }
}
