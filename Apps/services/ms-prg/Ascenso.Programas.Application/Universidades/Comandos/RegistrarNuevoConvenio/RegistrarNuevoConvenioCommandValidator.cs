using FluentValidation;

namespace Ascenso.Programas.Application.Universidades.Comandos.RegistrarNuevoConvenio;

public sealed class RegistrarNuevoConvenioCommandValidator : AbstractValidator<RegistrarNuevoConvenioCommand>
{
    public RegistrarNuevoConvenioCommandValidator()
    {
        RuleFor(x => x.UniversidadAliadaId).NotEmpty();
        RuleFor(x => x.NumeroConvenio).NotEmpty().MaximumLength(60);
        RuleFor(x => x.TerminosResumen).NotEmpty().MaximumLength(2000);

        RuleFor(x => x.FechaFin)
            .GreaterThan(x => x.FechaInicio)
            .WithErrorCode("FECHAS_CONVENIO_INVALIDAS")
            .WithMessage("La fecha de fin del convenio debe ser posterior a la fecha de inicio.");

        RuleFor(x => x.UrlDocumentoPdfFirebase)
            .NotEmpty()
            .WithErrorCode("DOCUMENTO_CONVENIO_PDF_REQUERIDO")
            .MaximumLength(1000)
            .Must(EsPdf)
            .WithErrorCode("DOCUMENTO_CONVENIO_PDF_REQUERIDO")
            .WithMessage("El documento del convenio en Firebase Storage es obligatorio y debe tener extensión '.pdf'.");

        RuleFor(x => x.UsuarioModificacionId).NotEmpty();
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
