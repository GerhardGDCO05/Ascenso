using FluentValidation;

namespace Ascenso.Programas.Application.Universidades.Comandos.ActualizarCostoMatricula;

public sealed class ActualizarCostoMatriculaCommandValidator : AbstractValidator<ActualizarCostoMatriculaCommand>
{
    public ActualizarCostoMatriculaCommandValidator()
    {
        RuleFor(x => x.UniversidadAliadaId).NotEmpty();
        RuleFor(x => x.CarreraElegibleId).NotEmpty();
        RuleFor(x => x.PeriodoAcademicoId).NotEmpty().MaximumLength(20);

        RuleFor(x => x.MontoReferenciaMatricula)
            .GreaterThan(0)
            .WithErrorCode("MONTO_MATRICULA_INVALIDO")
            .Must(monto => decimal.Round(monto, 2) == monto)
            .WithErrorCode("MONTO_MATRICULA_INVALIDO")
            .WithMessage("El monto de referencia de matrícula debe ser mayor a cero y tener como máximo dos decimales.");

        RuleFor(x => x.Moneda)
            .NotEmpty()
            .Must(moneda => string.Equals(moneda?.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
            .WithMessage("La moneda admitida para el costo de matrícula es USD.");

        RuleFor(x => x.UsuarioModificacionId).NotEmpty();
    }
}
