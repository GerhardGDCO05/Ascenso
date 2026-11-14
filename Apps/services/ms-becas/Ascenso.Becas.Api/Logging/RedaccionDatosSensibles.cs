using System.Globalization;
using System.Text;

namespace Ascenso.Becas.Api.Logging;

/// <summary>
/// Identifica claves de log que contienen datos sensibles y produce el valor
/// de reemplazo. La comparación ignora mayúsculas, espacios, guiones y acentos.
/// </summary>
public static class RedaccionDatosSensibles
{
    public const string Reemplazo = "[REDACTADO]";

    private static readonly string[] _clavesSensibles =
    [
        "otp",
        "codigoOtp",
        "codigoVerificacion",
        "cuenta",
        "cuentaBancaria",
        "numeroCuenta",
        "iban",
        "cbu",
        "documento",
        "documentoIdentidad",
        "cedula",
        "dni",
        "pasaporte",
        "correo",
        "correoElectronico",
        "email"
    ];

    private static readonly string[] _clavesNormalizadas = _clavesSensibles
        .Select(Normalizar)
        .ToArray();

    public static bool EsClaveSensible(string? clave)
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            return false;
        }

        var normalizada = Normalizar(clave);

        return _clavesNormalizadas.Any(sensible =>
            normalizada.Contains(sensible, StringComparison.Ordinal));
    }

    public static string Redactar(string? valor)
        => string.IsNullOrEmpty(valor) ? string.Empty : Reemplazo;

    private static string Normalizar(string valor)
    {
        var descompuesto = valor.Normalize(NormalizationForm.FormD);
        var constructor = new StringBuilder(descompuesto.Length);

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter)
                == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caracter))
            {
                constructor.Append(char.ToLowerInvariant(caracter));
            }
        }

        return constructor.ToString();
    }
}
