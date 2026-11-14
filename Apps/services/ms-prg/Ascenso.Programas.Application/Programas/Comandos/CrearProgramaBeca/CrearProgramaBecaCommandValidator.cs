using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.CrearProgramaBeca;

public sealed class CrearProgramaBecaCommandValidator : AbstractValidator<CrearProgramaBecaCommand>
{
    public CrearProgramaBecaCommandValidator()
    {
        RuleFor(cmd => cmd.Codigo)
            .NotEmpty()
            .MaximumLength(40);

        RuleFor(cmd => cmd.Nombre)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(cmd => cmd.Descripcion)
            .NotEmpty()
            .MaximumLength(1500);

        RuleFor(cmd => cmd.CupoTotal)
            .GreaterThan(0);

        RuleFor(cmd => cmd.DuracionPeriodos)
            .GreaterThan(0);

        RuleFor(cmd => cmd.PeriodoInicialId)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(cmd => cmd.VigenciaHasta)
            .GreaterThan(cmd => cmd.VigenciaDesde)
            .WithErrorCode(FechasVigenciaProgramaInvalidasException.CodigoError)
            .WithMessage("La fecha de fin de vigencia debe ser posterior a la fecha de inicio.");

        RuleFor(cmd => cmd.UsuarioCreacionId)
            .NotEqual(Guid.Empty);

        RuleFor(cmd => cmd.PoliticaCobertura)
            .NotNull()
            .WithErrorCode(PoliticaCoberturaInvalidaException.CodigoError);

        When(cmd => cmd.PoliticaCobertura is not null, () =>
        {
            RuleFor(cmd => cmd.PoliticaCobertura.Modalidad)
                .NotEmpty()
                .Must(EsModalidadValida)
                .WithErrorCode(PoliticaCoberturaInvalidaException.CodigoError)
                .WithMessage("La modalidad de cobertura debe ser TOTAL, PARCIAL o MONTO_FIJO.");

            RuleFor(cmd => cmd.PoliticaCobertura.Moneda)
                .NotEmpty()
                .Must(moneda => string.Equals(moneda.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
                .WithErrorCode(PoliticaCoberturaInvalidaException.CodigoError)
                .WithMessage("La moneda debe ser USD.");
        });
    }

    private static bool EsModalidadValida(string? modalidad)
    {
        if (string.IsNullOrWhiteSpace(modalidad))
        {
            return false;
        }

        try
        {
            _ = CodigoEnum.DesdeCodigo<ModalidadCobertura>(modalidad);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
