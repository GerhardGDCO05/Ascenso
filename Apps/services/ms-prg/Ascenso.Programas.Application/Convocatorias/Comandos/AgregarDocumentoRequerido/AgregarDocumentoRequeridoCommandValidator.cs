using Ascenso.Programas.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Programas.Application.Convocatorias.Comandos.AgregarDocumentoRequerido;

public sealed class AgregarDocumentoRequeridoCommandValidator : AbstractValidator<AgregarDocumentoRequeridoCommand>
{
    public AgregarDocumentoRequeridoCommandValidator()
    {
        RuleFor(cmd => cmd.ConvocatoriaId)
            .NotEqual(Guid.Empty);

        RuleFor(cmd => cmd.TipoDocumento)
            .NotEmpty()
            .MaximumLength(60)
            .WithErrorCode(DocumentoRequeridoInvalidoException.CodigoError);

        RuleFor(cmd => cmd.Nombre)
            .NotEmpty()
            .MaximumLength(200)
            .WithErrorCode(DocumentoRequeridoInvalidoException.CodigoError);

        RuleFor(cmd => cmd.Descripcion)
            .MaximumLength(1000)
            .WithErrorCode(DocumentoRequeridoInvalidoException.CodigoError);

        RuleFor(cmd => cmd.TamanioMaximoMb)
            .InclusiveBetween(1, 50)
            .WithErrorCode(DocumentoRequeridoInvalidoException.CodigoError);

        RuleFor(cmd => cmd.TiposMimePermitidos)
            .NotEmpty()
            .WithErrorCode(DocumentoRequeridoInvalidoException.CodigoError);

        RuleFor(cmd => cmd.UsuarioModificacionId)
            .NotEqual(Guid.Empty);
    }
}
