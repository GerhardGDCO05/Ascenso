using FluentValidation;

namespace Ascenso.Programas.Application.Universidades.Comandos.AgregarCarreraElegible;

public sealed class AgregarCarreraElegibleCommandValidator : AbstractValidator<AgregarCarreraElegibleCommand>
{
    public AgregarCarreraElegibleCommandValidator()
    {
        RuleFor(x => x.UniversidadAliadaId).NotEmpty();
        RuleFor(x => x.CodigoCarrera).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DuracionSemestres).GreaterThan(0);
        RuleFor(x => x.UsuarioModificacionId).NotEmpty();
    }
}
