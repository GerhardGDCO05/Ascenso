using FluentValidation;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.CargarCompromisoBecario;

/// <summary>
/// Valida el archivo antes de leer sus bytes en el caso de uso.
/// </summary>
public sealed class CargarCompromisoBecarioValidator
    : AbstractValidator<CargarCompromisoBecarioCommand>
{
    public const int TamanioMaximoBytes = 5 * 1024 * 1024;

    public CargarCompromisoBecarioValidator()
    {
        RuleFor(comando => comando.BecaId)
            .NotEmpty()
            .WithMessage("La beca es obligatoria.");

        RuleFor(comando => comando.NombreArchivo)
            .NotEmpty()
            .WithMessage("El nombre del archivo es obligatorio.")
            .MaximumLength(255)
            .WithMessage("El nombre del archivo no puede superar 255 caracteres.")
            .Must(nombre => string.Equals(
                Path.GetExtension(nombre),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
            .WithMessage("El archivo del compromiso debe tener extensión .pdf.");

        RuleFor(comando => comando.TipoMime)
            .NotEmpty()
            .WithMessage("El tipo MIME del archivo es obligatorio.")
            .Must(tipo => string.Equals(tipo, "application/pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("El archivo del compromiso debe ser PDF.");

        RuleFor(comando => comando.Contenido)
            .NotNull()
            .WithMessage("El contenido del archivo es obligatorio.")
            .Must(contenido => contenido is { Length: > 0 })
            .WithMessage("El archivo del compromiso no puede estar vacío.")
            .Must(contenido => contenido is null || contenido.Length <= TamanioMaximoBytes)
            .WithMessage("El archivo del compromiso no puede superar 5 MB.");
    }
}
