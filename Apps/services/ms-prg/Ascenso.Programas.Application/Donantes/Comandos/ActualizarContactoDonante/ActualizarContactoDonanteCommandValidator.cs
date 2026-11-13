using FluentValidation;

namespace Ascenso.Programas.Application.Donantes.Comandos.ActualizarContactoDonante;

public sealed class ActualizarContactoDonanteCommandValidator : AbstractValidator<ActualizarContactoDonanteCommand>
{
    public ActualizarContactoDonanteCommandValidator()
    {
        RuleFor(x => x.DonanteId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Telefono).NotEmpty().MaximumLength(40);
        RuleFor(x => x.DireccionFiscal).NotEmpty().MaximumLength(400);
        RuleFor(x => x.ContactoPrincipal)
            .NotNull()
            .ChildRules(contacto =>
            {
                contacto.RuleFor(c => c.NombreRepresentante).NotEmpty().MaximumLength(150);
                contacto.RuleFor(c => c.Cargo).NotEmpty().MaximumLength(120);
                contacto.RuleFor(c => c.EmailContacto).NotEmpty().EmailAddress().MaximumLength(200);
                contacto.RuleFor(c => c.TelefonoContacto).NotEmpty().MaximumLength(40);
            });
        RuleFor(x => x.UsuarioModificacionId).NotEmpty();
    }
}

