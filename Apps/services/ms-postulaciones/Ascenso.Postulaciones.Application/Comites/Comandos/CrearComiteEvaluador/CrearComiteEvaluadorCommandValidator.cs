using Ascenso.Postulaciones.Domain.Comites.Agregados;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Comites.Comandos.CrearComiteEvaluador;

public sealed class CrearComiteEvaluadorCommandValidator : AbstractValidator<CrearComiteEvaluadorCommand>
{
    public CrearComiteEvaluadorCommandValidator()
    {
        RuleFor(x => x.ProgramaBecaId)
            .NotEmpty()
            .WithMessage("El identificador del programa de beca es obligatorio.");

        RuleFor(x => x.ConvocatoriaId)
            .NotEmpty()
            .WithMessage("El identificador de la convocatoria es obligatorio.");

        RuleFor(x => x.JefaturaUsuarioId)
            .NotEmpty()
            .WithMessage("El identificador de la jefatura del comité es obligatorio.");

        RuleFor(x => x.Nombre)
            .NotEmpty()
            .WithMessage("El nombre del comité evaluador es obligatorio.")
            .Must(nombre => string.IsNullOrWhiteSpace(nombre)
                || nombre.Trim().Length <= ComiteEvaluador.LongitudMaximaNombre)
            .WithMessage($"El nombre del comité evaluador no puede superar los {ComiteEvaluador.LongitudMaximaNombre} caracteres.");
    }
}
