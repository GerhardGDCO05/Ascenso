using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Domain.Enums;
using FluentValidation;

namespace Ascenso.Programas.Application.Donantes.Comandos.RegistrarDonante;

public sealed class RegistrarDonanteCommandValidator : AbstractValidator<RegistrarDonanteCommand>
{
    public RegistrarDonanteCommandValidator()
    {
        RuleFor(x => x.TipoDonante)
            .NotEmpty()
            .Must(valor => DonanteMapeador.IntentarConvertirEnum<TipoDonante>(valor, out _))
            .WithMessage("El tipo de donante debe ser EMPRESA, ORGANIZACION o EGRESADO_PERSONA.");

        RuleFor(x => x.RazonSocialNombre)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.IdentificacionFiscal)
            .NotEmpty()
            .MaximumLength(40);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(x => x.Telefono)
            .NotEmpty()
            .MaximumLength(40);

        RuleFor(x => x.DireccionFiscal)
            .NotEmpty()
            .MaximumLength(400);

        RuleFor(x => x.ContactoPrincipal)
            .NotNull()
            .ChildRules(contacto =>
            {
                contacto.RuleFor(c => c.NombreRepresentante).NotEmpty().MaximumLength(150);
                contacto.RuleFor(c => c.Cargo).NotEmpty().MaximumLength(120);
                contacto.RuleFor(c => c.EmailContacto).NotEmpty().EmailAddress().MaximumLength(200);
                contacto.RuleFor(c => c.TelefonoContacto).NotEmpty().MaximumLength(40);
            });

        RuleFor(x => x.UsuarioCreacionId)
            .NotEmpty();
    }
}

