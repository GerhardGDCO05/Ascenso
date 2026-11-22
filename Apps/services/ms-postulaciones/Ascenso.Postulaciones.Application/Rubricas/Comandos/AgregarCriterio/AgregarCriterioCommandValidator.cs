using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.AgregarCriterio;

public sealed class AgregarCriterioCommandValidator : AbstractValidator<AgregarCriterioCommand>
{
    public AgregarCriterioCommandValidator()
    {
        RuleFor(x => x.RubricaId)
            .NotEmpty()
            .WithMessage("El identificador de la rúbrica de evaluación es obligatorio.");

        RuleFor(x => x.Nombre)
            .NotEmpty()
            .WithMessage("El nombre del criterio de evaluación es obligatorio.")
            .Must(nombre => string.IsNullOrWhiteSpace(nombre)
                || nombre.Trim().Length <= CriterioEvaluacion.LongitudMaximaNombre)
            .WithMessage($"El nombre del criterio de evaluación no puede superar los {CriterioEvaluacion.LongitudMaximaNombre} caracteres.");

        RuleFor(x => x.Descripcion)
            .NotEmpty()
            .WithMessage("La descripción del criterio de evaluación es obligatoria.")
            .Must(descripcion => string.IsNullOrWhiteSpace(descripcion)
                || descripcion.Trim().Length <= CriterioEvaluacion.LongitudMaximaDescripcion)
            .WithMessage($"La descripción del criterio de evaluación no puede superar los {CriterioEvaluacion.LongitudMaximaDescripcion} caracteres.");

        RuleFor(x => x.PesoPorcentual)
            .Must(peso => peso is > CriterioEvaluacion.PesoMinimoExclusivo and <= CriterioEvaluacion.PesoMaximo)
            .WithMessage("El peso porcentual debe ser mayor que 0 y menor o igual que 100.00.")
            .Must(TieneMaximoDosDecimales)
            .WithMessage($"El peso porcentual admite como máximo {CriterioEvaluacion.DecimalesMaximos} decimales.");

        RuleFor(x => x.PuntajeMinimo)
            .Must(puntaje => puntaje is >= CriterioEvaluacion.PuntajeMinimoPermitido and <= CriterioEvaluacion.PuntajeMaximoPermitido)
            .WithMessage("El puntaje mínimo debe estar entre 0.00 y 1000.00.")
            .Must(TieneMaximoDosDecimales)
            .WithMessage($"El puntaje mínimo admite como máximo {CriterioEvaluacion.DecimalesMaximos} decimales.");

        RuleFor(x => x.PuntajeMaximo)
            .Must(puntaje => puntaje is >= CriterioEvaluacion.PuntajeMinimoPermitido and <= CriterioEvaluacion.PuntajeMaximoPermitido)
            .WithMessage("El puntaje máximo debe estar entre 0.00 y 1000.00.")
            .Must(TieneMaximoDosDecimales)
            .WithMessage($"El puntaje máximo admite como máximo {CriterioEvaluacion.DecimalesMaximos} decimales.")
            .Must((comando, puntajeMaximo) => comando.PuntajeMinimo < puntajeMaximo)
            .WithMessage("El puntaje mínimo debe ser estrictamente menor que el puntaje máximo.");
    }

    private static bool TieneMaximoDosDecimales(decimal valor)
        => decimal.Round(valor, CriterioEvaluacion.DecimalesMaximos) == valor;
}
