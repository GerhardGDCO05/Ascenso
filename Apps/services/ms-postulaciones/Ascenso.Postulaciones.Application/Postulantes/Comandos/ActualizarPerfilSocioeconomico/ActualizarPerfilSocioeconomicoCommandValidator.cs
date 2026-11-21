using Ascenso.Postulaciones.Application.Postulantes.DTOs;
using FluentValidation;
using OcupacionEnum = Ascenso.Postulaciones.Domain.Enums.Ocupacion;

namespace Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;

public sealed class ActualizarPerfilSocioeconomicoCommandValidator
    : AbstractValidator<ActualizarPerfilSocioeconomicoCommand>
{
    private const int DecimalesMaximos = 2;

    public ActualizarPerfilSocioeconomicoCommandValidator()
    {
        RuleFor(x => x.EstadoCivil)
            .NotEmpty()
            .Must(valor => PostulanteMapeador.IntentarConvertirEstadoCivil(valor, out _))
            .WithMessage("El estado civil debe ser SOLTERO, CASADO, DIVORCIADO o VIUDO.");

        RuleFor(x => x.PromedioActual)
            .NotNull()
            .WithMessage("El promedio actual es obligatorio.")
            .Must(valor => !valor.HasValue || valor.Value is >= 0m and <= 20m)
            .WithMessage("El promedio actual debe estar entre 0.00 y 20.00.")
            .Must(valor => !valor.HasValue || TieneMaximoDosDecimales(valor.Value))
            .WithMessage("El promedio actual admite como máximo dos decimales.");

        RuleFor(x => x.Trabaja)
            .NotNull()
            .WithMessage("La condición laboral es obligatoria.");

        RuleFor(x => x.Ocupacion)
            .NotEmpty()
            .Must(valor => PostulanteMapeador.IntentarConvertirOcupacion(valor, out _))
            .WithMessage("La ocupación no corresponde a un valor válido del catálogo.")
            .Must((comando, valor) => EsCoherente(comando.Trabaja, valor))
            .WithMessage("La ocupación no es coherente con la condición laboral declarada.");

        RuleFor(x => x.IngresoMensualPostulante)
            .NotNull()
            .WithMessage("El ingreso mensual del postulante es obligatorio.")
            .Must(valor => !valor.HasValue || valor.Value >= 0m)
            .WithMessage("El ingreso mensual del postulante no puede ser negativo.")
            .Must(valor => !valor.HasValue || TieneMaximoDosDecimales(valor.Value))
            .WithMessage("El ingreso mensual del postulante admite como máximo dos decimales.");

        RuleFor(x => x.MiembrosFamiliares)
            .NotNull()
            .WithMessage("La lista de miembros familiares es obligatoria.");

        RuleForEach(x => x.MiembrosFamiliares)
            .SetValidator(new DatosMiembroFamiliarCommandValidator());
    }

    private static bool EsCoherente(bool? trabaja, string? codigoOcupacion)
    {
        if (!trabaja.HasValue
            || !PostulanteMapeador.IntentarConvertirOcupacion(codigoOcupacion, out var ocupacion))
        {
            return true;
        }

        return trabaja.Value
            ? ocupacion is OcupacionEnum.Empleado
                or OcupacionEnum.Independiente
                or OcupacionEnum.Otro
            : ocupacion is OcupacionEnum.Estudiante
                or OcupacionEnum.LaboresHogar
                or OcupacionEnum.Jubilado
                or OcupacionEnum.NoTrabaja
                or OcupacionEnum.Otro;
    }

    private static bool TieneMaximoDosDecimales(decimal valor)
        => decimal.Round(valor, DecimalesMaximos) == valor;
}
