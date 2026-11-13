using FluentValidation;

namespace Ascenso.Programas.Application.Donantes.Comandos.RegistrarAporteDonante;

public sealed class RegistrarAporteDonanteCommandValidator : AbstractValidator<RegistrarAporteDonanteCommand>
{
    public RegistrarAporteDonanteCommandValidator()
    {
        RuleFor(x => x.DonanteId).NotEmpty();

        RuleFor(x => x.MontoAporte)
            .GreaterThan(0)
            .WithErrorCode("MONTO_APORTE_INVALIDO")
            .Must(monto => decimal.Round(monto, 2) == monto)
            .WithErrorCode("MONTO_APORTE_INVALIDO")
            .WithMessage("El monto del aporte debe ser mayor a cero y tener como máximo dos decimales.");

        RuleFor(x => x.Moneda)
            .NotEmpty()
            .Must(moneda => string.Equals(moneda?.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
            .WithMessage("La divisa del aporte debe ser USD.");

        RuleFor(x => x.PeriodoAcademicoId)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.FechaAporte)
            .Must(fecha => fecha != default)
            .WithMessage("La fecha de transacción del aporte es obligatoria.");

        RuleFor(x => x.BancoEmisor)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.NumeroReferenciaBancaria)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.UrlComprobanteFirebase)
            .NotEmpty()
            .WithErrorCode("COMPROBANTE_REQUERIDO")
            .MaximumLength(1000)
            .Must((comando, url) =>
                !string.IsNullOrWhiteSpace(url)
                && Uri.UnescapeDataString(url.Trim()).Contains(
                    $"donantes/{comando.DonanteId:D}/aportes/",
                    StringComparison.OrdinalIgnoreCase))
            .WithErrorCode("COMPROBANTE_REQUERIDO")
            .WithMessage("El comprobante en Firebase Storage es obligatorio y debe corresponder a '/donantes/{donanteId}/aportes/{uuid}.pdf'.");

        RuleFor(x => x.Observaciones)
            .MaximumLength(1000);

        RuleFor(x => x.RegistradoPorUsuarioId)
            .NotEmpty();
    }
}

