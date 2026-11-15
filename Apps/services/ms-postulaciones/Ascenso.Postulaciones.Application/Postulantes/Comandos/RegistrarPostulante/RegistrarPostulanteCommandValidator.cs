using Ascenso.Postulaciones.Application.Postulantes.DTOs;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;

public sealed class RegistrarPostulanteCommandValidator : AbstractValidator<RegistrarPostulanteCommand>
{
    public RegistrarPostulanteCommandValidator()
    {
        RuleFor(x => x.NumeroDocumentoIdentidad)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(30);

        RuleFor(x => x.Nombres)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Apellidos)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.DatosContacto)
            .NotNull()
            .ChildRules(contacto =>
            {
                contacto.RuleFor(c => c.CorreoElectronico)
                    .NotEmpty()
                    .EmailAddress()
                    .MaximumLength(254);

                contacto.RuleFor(c => c.Telefono)
                    .NotEmpty()
                    .MaximumLength(30);
            });

        RuleFor(x => x.UniversidadAliadaId)
            .NotEmpty();

        RuleFor(x => x.CarreraId)
            .NotEmpty();

        RuleFor(x => x.PeriodoAcademicoId)
            .NotEmpty();

        RuleFor(x => x.CondicionAcademica)
            .NotEmpty()
            .Must(valor => PostulanteMapeador.IntentarConvertirCondicionAcademica(valor, out _))
            .WithMessage("La condición académica debe ser ADMITIDO o MATRICULADO.");
    }
}
