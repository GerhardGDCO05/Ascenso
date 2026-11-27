using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.RevisarDiscrepanciaConsolidacion;

public sealed class RevisarDiscrepanciaConsolidacionCommandValidator
    : AbstractValidator<RevisarDiscrepanciaConsolidacionCommand>
{
    public RevisarDiscrepanciaConsolidacionCommandValidator()
    {
        RuleFor(x => x.PostulacionId)
            .NotEmpty()
            .WithErrorCode(ConsolidacionInvalidaException.CodigoError)
            .WithMessage("El identificador de la postulación es obligatorio.");

        RuleFor(x => x.Comentario)
            .NotEmpty()
            .WithErrorCode(ComentarioRevisionDiscrepanciaInvalidoException.CodigoError)
            .WithMessage("El comentario de revisión de discrepancia es obligatorio.")
            .Must(comentario =>
            {
                if (string.IsNullOrWhiteSpace(comentario))
                {
                    return true;
                }

                var longitud = comentario.Trim().Length;
                return longitud >= ResultadoConsolidadoRanking.LongitudMinimaComentarioRevision
                    && longitud <= ResultadoConsolidadoRanking.LongitudMaximaComentarioRevision;
            })
            .WithErrorCode(ComentarioRevisionDiscrepanciaInvalidoException.CodigoError)
            .WithMessage(
                $"El comentario de revisión debe tener entre {ResultadoConsolidadoRanking.LongitudMinimaComentarioRevision} y {ResultadoConsolidadoRanking.LongitudMaximaComentarioRevision} caracteres.");
    }
}
