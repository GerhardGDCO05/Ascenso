using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.ConfigurarCriteriosElegibilidad;

public sealed class ConfigurarCriteriosElegibilidadCommandValidator
    : AbstractValidator<ConfigurarCriteriosElegibilidadCommand>
{
    public ConfigurarCriteriosElegibilidadCommandValidator()
    {
        RuleFor(cmd => cmd.ProgramaBecaId)
            .NotEqual(Guid.Empty);

        RuleFor(cmd => cmd.UsuarioModificacionId)
            .NotEqual(Guid.Empty);

        RuleFor(cmd => cmd.CriteriosElegibilidad)
            .NotNull();

        When(cmd => cmd.CriteriosElegibilidad is not null, () =>
        {
            RuleFor(cmd => cmd.CriteriosElegibilidad.PromedioMinimoIngreso)
                .InclusiveBetween(0.00m, 20.00m)
                .WithErrorCode(PromedioAcademicoInvalidoException.CodigoError);

            RuleFor(cmd => cmd.CriteriosElegibilidad.EdadMinima)
                .GreaterThan(0);

            RuleFor(cmd => cmd.CriteriosElegibilidad.EdadMaxima)
                .GreaterThanOrEqualTo(cmd => cmd.CriteriosElegibilidad.EdadMinima);

            RuleFor(cmd => cmd.CriteriosElegibilidad.TopeIngresoFamiliarMensual)
                .GreaterThan(0.00m);

            RuleFor(cmd => cmd.CriteriosElegibilidad.UniversidadesElegiblesIds)
                .NotEmpty()
                .Must(ids => ids.All(id => id != Guid.Empty))
                .WithMessage("Debe especificar al menos una universidad elegible válida.");

            RuleFor(cmd => cmd.CriteriosElegibilidad.CarrerasElegiblesIds)
                .NotEmpty()
                .Must(ids => ids.All(id => id != Guid.Empty))
                .WithMessage("Debe especificar al menos una carrera elegible válida.");
        });

        RuleFor(cmd => cmd.CondicionesPermanencia)
            .NotNull();

        When(cmd => cmd.CondicionesPermanencia is not null, () =>
        {
            RuleFor(cmd => cmd.CondicionesPermanencia.PromedioMinimoRenovacion)
                .InclusiveBetween(0.00m, 20.00m)
                .WithErrorCode(PromedioAcademicoInvalidoException.CodigoError);

            RuleFor(cmd => cmd.CondicionesPermanencia.MateriasMinimasAprobadas)
                .GreaterThanOrEqualTo(0);

            RuleFor(cmd => cmd.CondicionesPermanencia.MaximoMateriasReprobadas)
                .GreaterThanOrEqualTo(0);

            RuleFor(cmd => cmd.CondicionesPermanencia.HorasServicioPorPeriodo)
                .GreaterThanOrEqualTo(0);

            RuleFor(cmd => cmd.CondicionesPermanencia.MargenRiesgoPromedio)
                .InclusiveBetween(0.00m, 20.00m);

            RuleFor(cmd => cmd.CondicionesPermanencia.DuracionMaximaPausaPeriodos)
                .GreaterThanOrEqualTo(0);

            RuleFor(cmd => cmd.CondicionesPermanencia.FrecuenciaReportes)
                .NotEmpty()
                .Must(EsEnumValido<FrecuenciaReporteAcademico>);

            RuleFor(cmd => cmd.CondicionesPermanencia.ConsecuenciaIncumplimiento)
                .NotEmpty()
                .Must(EsEnumValido<ConsecuenciaIncumplimiento>);
        });
    }

    private static bool EsEnumValido<TEnum>(string? codigo)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return false;
        }

        try
        {
            _ = CodigoEnum.DesdeCodigo<TEnum>(codigo);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
