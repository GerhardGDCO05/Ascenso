using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;

public sealed class GuardarBorradorEvaluacionCommandValidator
    : AbstractValidator<GuardarBorradorEvaluacionCommand>
{
    public GuardarBorradorEvaluacionCommandValidator()
    {
        RuleFor(x => x.AsignacionEvaluacionId)
            .NotEmpty()
            .WithMessage("El identificador de la asignación de evaluación es obligatorio.");

        RuleFor(x => x.Calificaciones)
            .NotNull()
            .WithMessage("Debe proporcionar al menos una calificación de criterio.")
            .Must(calificaciones => calificaciones is { Count: > 0 })
            .WithMessage("Debe proporcionar al menos una calificación de criterio.")
            .Must(NoTenerCriteriosDuplicados)
            .WithMessage("No se permiten calificaciones duplicadas para un mismo criterio.");

        RuleForEach(x => x.Calificaciones)
            .NotNull()
            .WithMessage("Cada calificación de criterio es obligatoria.")
            .ChildRules(calificacion =>
            {
                calificacion.RuleFor(item => item.CriterioEvaluacionId)
                    .NotEmpty()
                    .WithMessage("El identificador del criterio de evaluación es obligatorio.");

                calificacion.RuleFor(item => item.PuntajeAsignado)
                    .InclusiveBetween(
                        CriterioEvaluacion.PuntajeMinimoPermitido,
                        CriterioEvaluacion.PuntajeMaximoPermitido)
                    .WithMessage(
                        $"El puntaje asignado debe estar entre {CriterioEvaluacion.PuntajeMinimoPermitido:0.00} y {CriterioEvaluacion.PuntajeMaximoPermitido:0.00}.")
                    .Must(TieneMaximoDosDecimales)
                    .WithMessage(
                        $"El puntaje asignado admite como máximo {DatosCalificacionCriterio.DecimalesMaximos} decimales.");

                calificacion.RuleFor(item => item.Comentario)
                    .Must(comentario => comentario!.Trim().Length <= DatosCalificacionCriterio.LongitudMaximaComentario)
                    .WithMessage(
                        $"El comentario por criterio no puede superar los {DatosCalificacionCriterio.LongitudMaximaComentario} caracteres.")
                    .When(item => !string.IsNullOrWhiteSpace(item.Comentario));
            });

        RuleFor(x => x.ComentariosGenerales)
            .Must(comentarios => comentarios!.Trim().Length <= Evaluacion.LongitudMaximaComentariosGenerales)
            .WithMessage(
                $"Los comentarios generales no pueden superar los {Evaluacion.LongitudMaximaComentariosGenerales} caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ComentariosGenerales));
    }

    private static bool NoTenerCriteriosDuplicados(
        IReadOnlyList<DatosCalificacionCriterioCommand>? calificaciones)
    {
        if (calificaciones is null)
        {
            return true;
        }

        var vistos = new HashSet<Guid>(calificaciones.Count);
        foreach (var item in calificaciones)
        {
            if (item is null || item.CriterioEvaluacionId == Guid.Empty)
            {
                continue;
            }

            if (!vistos.Add(item.CriterioEvaluacionId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TieneMaximoDosDecimales(decimal puntaje)
        => decimal.Round(puntaje, DatosCalificacionCriterio.DecimalesMaximos) == puntaje;
}
