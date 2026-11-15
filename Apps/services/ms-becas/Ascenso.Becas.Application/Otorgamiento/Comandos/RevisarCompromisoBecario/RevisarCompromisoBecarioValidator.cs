using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.RevisarCompromisoBecario;

/// <summary>
/// Valida la revisión: el rechazo exige observaciones y ambas decisiones
/// respetan la longitud máxima persistida.
/// </summary>
public sealed class RevisarCompromisoBecarioValidator
    : AbstractValidator<RevisarCompromisoBecarioCommand>
{
    public RevisarCompromisoBecarioValidator()
    {
        RuleFor(comando => comando.CompromisoId)
            .NotEmpty()
            .WithMessage("El compromiso es obligatorio.");

        RuleFor(comando => comando.Observaciones)
            .MaximumLength(1000)
            .WithMessage("Las observaciones no pueden superar 1000 caracteres.");

        RuleFor(comando => comando.Observaciones)
            .NotEmpty()
            .WithMessage("Las observaciones son obligatorias cuando se rechaza el compromiso.")
            .When(comando => !comando.Aprobar);
    }
}
