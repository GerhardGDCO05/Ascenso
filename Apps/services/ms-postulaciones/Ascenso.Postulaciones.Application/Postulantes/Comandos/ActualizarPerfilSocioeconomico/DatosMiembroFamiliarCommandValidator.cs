using Ascenso.Postulaciones.Application.Postulantes.DTOs;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;

public sealed class DatosMiembroFamiliarCommandValidator
    : AbstractValidator<DatosMiembroFamiliarCommand>
{
    private const int DecimalesMaximos = 2;

    public DatosMiembroFamiliarCommandValidator()
    {
        RuleFor(x => x.Parentesco)
            .NotEmpty()
            .Must(valor => PostulanteMapeador.IntentarConvertirParentesco(valor, out _))
            .WithMessage("El parentesco debe ser PADRE, MADRE, HERMANO, HIJO, PAREJA u OTRO.");

        RuleFor(x => x.Edad)
            .NotNull()
            .WithMessage("La edad del miembro familiar es obligatoria.")
            .Must(valor => !valor.HasValue || valor.Value is >= 0 and <= 120)
            .WithMessage("La edad del miembro familiar debe estar entre 0 y 120 años.");

        RuleFor(x => x.Ocupacion)
            .NotEmpty()
            .Must(valor => PostulanteMapeador.IntentarConvertirOcupacion(valor, out _))
            .WithMessage("La ocupación no corresponde a un valor válido del catálogo.");

        RuleFor(x => x.IngresoMensual)
            .NotNull()
            .WithMessage("El ingreso mensual del miembro familiar es obligatorio.")
            .Must(valor => !valor.HasValue || valor.Value >= 0m)
            .WithMessage("El ingreso mensual del miembro familiar no puede ser negativo.")
            .Must(valor => !valor.HasValue || TieneMaximoDosDecimales(valor.Value))
            .WithMessage("El ingreso mensual del miembro familiar admite como máximo dos decimales.");
    }

    private static bool TieneMaximoDosDecimales(decimal valor)
        => decimal.Round(valor, DecimalesMaximos) == valor;
}
