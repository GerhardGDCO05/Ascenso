namespace Ascenso.Programas.Domain.Universidades.ObjetosValor;

/// <summary>
/// Datos del enlace institucional de una universidad aliada.
/// </summary>
public sealed record class ContactoEnlace
{
    private ContactoEnlace()
    {
    }

    public ContactoEnlace(string nombre, string email, string telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del contacto de enlace es obligatorio.", nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("El correo electrónico del contacto de enlace debe ser válido.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(telefono))
        {
            throw new ArgumentException("El teléfono del contacto de enlace es obligatorio.", nameof(telefono));
        }

        Nombre = nombre.Trim();
        Email = email.Trim();
        Telefono = telefono.Trim();
    }

    public string Nombre { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Telefono { get; private set; } = string.Empty;
}
