using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.ResolverCasoLimiteRanking;

public sealed class ResolverCasoLimiteRankingCommandValidator
    : AbstractValidator<ResolverCasoLimiteRankingCommand>
{
    public ResolverCasoLimiteRankingCommandValidator()
    {
        RuleFor(x => x.ConvocatoriaId)
            .NotEmpty()
            .WithErrorCode(RankingInvalidoException.CodigoError)
            .WithMessage("El identificador de la convocatoria es obligatorio.");

        RuleFor(x => x.PostulacionesSeleccionadasIds)
            .NotNull()
            .WithErrorCode(SeleccionCasoLimiteInvalidaException.CodigoError)
            .WithMessage("Debe especificar las postulaciones seleccionadas para resolver el caso límite.")
            .NotEmpty()
            .WithErrorCode(SeleccionCasoLimiteInvalidaException.CodigoError)
            .WithMessage("Debe seleccionar al menos una postulación del grupo afectado.")
            .Must(ids => ids is null || ids.All(id => id != Guid.Empty))
            .WithErrorCode(SeleccionCasoLimiteInvalidaException.CodigoError)
            .WithMessage("Las postulaciones seleccionadas no pueden contener identificadores vacíos.")
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithErrorCode(SeleccionCasoLimiteInvalidaException.CodigoError)
            .WithMessage("Las postulaciones seleccionadas no pueden contener duplicados.");

        RuleFor(x => x.Justificacion)
            .NotEmpty()
            .WithErrorCode(JustificacionCasoLimiteInvalidaException.CodigoError)
            .WithMessage("La justificación de resolución del caso límite es obligatoria.")
            .Must(justificacion =>
            {
                if (string.IsNullOrWhiteSpace(justificacion))
                {
                    return true;
                }

                var longitud = justificacion.Trim().Length;
                return longitud >= ResultadoConsolidadoRanking.LongitudMinimaJustificacionCasoLimite
                    && longitud <= ResultadoConsolidadoRanking.LongitudMaximaJustificacionCasoLimite;
            })
            .WithErrorCode(JustificacionCasoLimiteInvalidaException.CodigoError)
            .WithMessage(
                $"La justificación de resolución del caso límite debe tener entre {ResultadoConsolidadoRanking.LongitudMinimaJustificacionCasoLimite} y {ResultadoConsolidadoRanking.LongitudMaximaJustificacionCasoLimite} caracteres.");
    }
}
