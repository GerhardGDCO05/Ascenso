using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Donantes.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable con los datos del representante legal o contacto principal del donante.
/// </summary>
public sealed record class ContactoDonante : ValueObject
{
    public ContactoDonante(
        string nombreRepresentante,
        string cargo,
        string emailContacto,
        string telefonoContacto)
    {
        if (string.IsNullOrWhiteSpace(nombreRepresentante))
        {
            throw new ArgumentException("El nombre del representante legal o contacto es obligatorio.", nameof(nombreRepresentante));
        }

        if (string.IsNullOrWhiteSpace(cargo))
        {
            throw new ArgumentException("El cargo del contacto principal es obligatorio.", nameof(cargo));
        }

        if (string.IsNullOrWhiteSpace(emailContacto) || !emailContacto.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("El correo electrónico de contacto debe tener un formato válido.", nameof(emailContacto));
        }

        if (string.IsNullOrWhiteSpace(telefonoContacto))
        {
            throw new ArgumentException("El teléfono de contacto es obligatorio.", nameof(telefonoContacto));
        }

        NombreRepresentante = nombreRepresentante.Trim();
        Cargo = cargo.Trim();
        EmailContacto = emailContacto.Trim();
        TelefonoContacto = telefonoContacto.Trim();
    }

    public string NombreRepresentante { get; }

    public string Cargo { get; }

    public string EmailContacto { get; }

    public string TelefonoContacto { get; }
}

