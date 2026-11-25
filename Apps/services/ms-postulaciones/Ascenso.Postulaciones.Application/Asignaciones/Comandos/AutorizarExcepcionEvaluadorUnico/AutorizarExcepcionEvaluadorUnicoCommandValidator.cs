using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Comandos.AutorizarExcepcionEvaluadorUnico;

public sealed class AutorizarExcepcionEvaluadorUnicoCommandValidator
    : AbstractValidator<AutorizarExcepcionEvaluadorUnicoCommand>
{
    public AutorizarExcepcionEvaluadorUnicoCommandValidator()
    {
        RuleFor(x => x.PostulacionId)
            .NotEmpty()
            .WithErrorCode("VALIDACION_INVALIDA")
            .WithMessage("El identificador de la postulación es obligatorio.");

        RuleFor(x => x.Justificacion)
            .NotEmpty()
            .WithErrorCode(JustificacionExcepcionInvalidaException.CodigoError)
            .WithMessage("La justificación de la excepción de evaluador único es obligatoria.")
            .Must(justificacion =>
            {
                if (string.IsNullOrWhiteSpace(justificacion))
                {
                    return true;
                }

                var longitud = justificacion.Trim().Length;
                return longitud >= AutorizacionExcepcionEvaluadorUnico.LongitudMinimaJustificacion
                    && longitud <= AutorizacionExcepcionEvaluadorUnico.LongitudMaximaJustificacion;
            })
            .WithErrorCode(JustificacionExcepcionInvalidaException.CodigoError)
            .WithMessage(
                $"La justificación debe tener entre {AutorizacionExcepcionEvaluadorUnico.LongitudMinimaJustificacion} y {AutorizacionExcepcionEvaluadorUnico.LongitudMaximaJustificacion} caracteres.");
    }
}
