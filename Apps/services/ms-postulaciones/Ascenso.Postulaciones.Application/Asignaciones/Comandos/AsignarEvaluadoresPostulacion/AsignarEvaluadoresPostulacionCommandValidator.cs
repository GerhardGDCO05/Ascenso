using Ascenso.Postulaciones.Domain.Asignaciones.Servicios;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Comandos.AsignarEvaluadoresPostulacion;

public sealed class AsignarEvaluadoresPostulacionCommandValidator
    : AbstractValidator<AsignarEvaluadoresPostulacionCommand>
{
    public AsignarEvaluadoresPostulacionCommandValidator()
    {
        RuleFor(x => x.ComiteId)
            .NotEmpty()
            .WithMessage("El identificador del comité evaluador es obligatorio.");

        RuleFor(x => x.PostulacionId)
            .NotEmpty()
            .WithMessage("El identificador de la postulación es obligatorio.");

        RuleFor(x => x.FechaLimiteEvaluacion)
            .NotEmpty()
            .WithMessage("La fecha límite de evaluación es obligatoria.");

        RuleFor(x => x)
            .Must(comando =>
            {
                var tieneLista = comando.UsuariosEvaluadoresIds is { Count: > 0 };
                var tieneCantidad = comando.CantidadEvaluadores.HasValue;
                return tieneLista || tieneCantidad;
            })
            .WithMessage("Debe indicar la lista de evaluadores o la cantidad de evaluadores para asignación automática.");

        RuleFor(x => x)
            .Must(comando =>
            {
                var tieneLista = comando.UsuariosEvaluadoresIds is { Count: > 0 };
                var tieneCantidad = comando.CantidadEvaluadores.HasValue;
                return !(tieneLista && tieneCantidad);
            })
            .WithMessage("Debe indicar la lista de evaluadores o la cantidad de evaluadores, pero no ambos.");

        RuleForEach(x => x.UsuariosEvaluadoresIds)
            .NotEmpty()
            .WithMessage("Los identificadores de los evaluadores no pueden estar vacíos.")
            .When(x => x.UsuariosEvaluadoresIds is not null);

        RuleFor(x => x.CantidadEvaluadores)
            .GreaterThanOrEqualTo(ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion)
            .WithMessage(
                $"La cantidad de evaluadores para asignación automática debe ser al menos {ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion}.")
            .When(x => x.CantidadEvaluadores.HasValue);
    }
}
