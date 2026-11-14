using FluentValidation;

namespace Ascenso.Programas.Application.Universidades.Comandos.RegistrarUniversidadConvenio;

public sealed class RegistrarUniversidadConvenioCommandValidator : AbstractValidator<RegistrarUniversidadConvenioCommand>
{
    public RegistrarUniversidadConvenioCommandValidator()
    {
        RuleFor(x => x.CodigoInstitucional)
            .NotEmpty()
            .MaximumLength(40);

        RuleFor(x => x.Nombre)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Direccion)
            .NotEmpty()
            .MaximumLength(400);

        RuleFor(x => x.ContactoEnlace)
            .NotNull()
            .ChildRules(contacto =>
            {
                contacto.RuleFor(c => c.Nombre).NotEmpty().MaximumLength(150);
                contacto.RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(200);
                contacto.RuleFor(c => c.Telefono).NotEmpty().MaximumLength(40);
            });

        When(x => x.CuentaBancariaInstitucional is not null, () =>
        {
            RuleFor(x => x.CuentaBancariaInstitucional!)
                .ChildRules(cuenta =>
                {
                    cuenta.RuleFor(c => c.Banco).NotEmpty().MaximumLength(120);
                    cuenta.RuleFor(c => c.TipoCuenta).NotEmpty().MaximumLength(40);
                    cuenta.RuleFor(c => c.NumeroCuentaCifrado).NotEmpty().MaximumLength(200);
                    cuenta.RuleFor(c => c.RifTitular).NotEmpty().MaximumLength(40);
                });
        });

        RuleFor(x => x.NumeroConvenio)
            .NotEmpty()
            .MaximumLength(60);

        RuleFor(x => x.TerminosResumen)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.FechaFinConvenio)
            .GreaterThan(x => x.FechaInicioConvenio)
            .WithErrorCode("FECHAS_CONVENIO_INVALIDAS")
            .WithMessage("La fecha de fin del convenio debe ser posterior a la fecha de inicio.");

        RuleFor(x => x.UrlDocumentoPdfFirebase)
            .NotEmpty()
            .WithErrorCode("DOCUMENTO_CONVENIO_PDF_REQUERIDO")
            .MaximumLength(1000)
            .Must(EsPdf)
            .WithErrorCode("DOCUMENTO_CONVENIO_PDF_REQUERIDO")
            .WithMessage("El documento del convenio en Firebase Storage es obligatorio y debe tener extensión '.pdf'.");

        RuleFor(x => x.UsuarioCreacionId)
            .NotEmpty();
    }

    private static bool EsPdf(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var limpia = url.Trim();
        var indiceQuery = limpia.IndexOfAny(['?', '#']);
        if (indiceQuery >= 0)
        {
            limpia = limpia[..indiceQuery];
        }

        return limpia.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }
}
