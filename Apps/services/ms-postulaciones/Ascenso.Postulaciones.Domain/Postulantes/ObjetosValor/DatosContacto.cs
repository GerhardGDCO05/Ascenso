using System.Net.Mail;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;

/// <summary>
/// Datos de contacto declarados por el postulante para las comunicaciones
/// oficiales de la Fundación.
/// </summary>
public sealed record class DatosContacto : ValueObject
{
    private const int LongitudMaximaCorreo = 254;
    private const int LongitudMinimaTelefono = 7;
    private const int LongitudMaximaTelefono = 15;

    public DatosContacto(string correoElectronico, string telefono)
    {
        CorreoElectronico = NormalizarCorreo(correoElectronico);
        Telefono = NormalizarTelefono(telefono);
    }

    public string CorreoElectronico { get; }

    public string Telefono { get; }

    private static string NormalizarCorreo(string correoElectronico)
    {
        if (string.IsNullOrWhiteSpace(correoElectronico))
        {
            throw new DatosPersonalesInvalidosException("El correo electrónico es obligatorio.");
        }

        var correoNormalizado = correoElectronico.Trim();

        if (correoNormalizado.Length > LongitudMaximaCorreo
            || !MailAddress.TryCreate(correoNormalizado, out var direccion)
            || direccion is null
            || !string.Equals(direccion.Address, correoNormalizado, StringComparison.Ordinal))
        {
            throw new DatosPersonalesInvalidosException(
                "El correo electrónico no tiene un formato válido.");
        }

        return correoNormalizado;
    }

    private static string NormalizarTelefono(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
        {
            throw new DatosPersonalesInvalidosException("El teléfono es obligatorio.");
        }

        var telefonoNormalizado = telefono.Trim();
        var digitos = new char[telefonoNormalizado.Length + 1];
        var posicion = 0;

        for (var indice = 0; indice < telefonoNormalizado.Length; indice++)
        {
            var caracter = telefonoNormalizado[indice];

            if (caracter == '+')
            {
                if (indice != 0)
                {
                    throw new DatosPersonalesInvalidosException(
                        "El teléfono solo admite el prefijo '+' al inicio.");
                }

                digitos[posicion++] = caracter;
                continue;
            }

            if (char.IsDigit(caracter))
            {
                digitos[posicion++] = caracter;
                continue;
            }

            if (caracter is not (' ' or '-' or '(' or ')' or '.'))
            {
                throw new DatosPersonalesInvalidosException(
                    "El teléfono contiene caracteres no permitidos.");
            }
        }

        var telefonoLimpio = new string(digitos, 0, posicion);
        var cantidadDigitos = telefonoLimpio.Count(char.IsDigit);

        if (cantidadDigitos is < LongitudMinimaTelefono or > LongitudMaximaTelefono)
        {
            throw new DatosPersonalesInvalidosException(
                $"El teléfono debe contener entre {LongitudMinimaTelefono} y {LongitudMaximaTelefono} dígitos.");
        }

        return telefonoLimpio;
    }
}
