using System.Text;

namespace Ascenso.Programas.Domain.Enums;

/// <summary>
/// Traduce los nombres PascalCase de C# al código literal en MAYÚSCULAS
/// usado por PostgreSQL y los contratos de integración.
/// </summary>
public static class CodigoEnum
{
    public static string ACodigo<TEnum>(this TEnum valor)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(valor))
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "El valor de enumeración no es válido.");
        }

        var nombre = valor.ToString();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "El valor de enumeración no es válido.");
        }

        var codigo = new StringBuilder(nombre.Length + 4);

        for (var indice = 0; indice < nombre.Length; indice++)
        {
            var caracter = nombre[indice];

            if (indice > 0
                && char.IsUpper(caracter)
                && (char.IsLower(nombre[indice - 1]) || char.IsDigit(nombre[indice - 1])))
            {
                codigo.Append('_');
            }

            codigo.Append(char.ToUpperInvariant(caracter));
        }

        return codigo.ToString();
    }

    public static TEnum DesdeCodigo<TEnum>(string codigo)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException($"El código de enumeración para {typeof(TEnum).Name} es obligatorio.", nameof(codigo));
        }

        var codigoNormalizado = codigo.Trim().ToUpperInvariant();

        foreach (var valor in Enum.GetValues<TEnum>())
        {
            if (string.Equals(valor.ACodigo(), codigoNormalizado, StringComparison.Ordinal))
            {
                return valor;
            }
        }

        throw new ArgumentException(
            $"El código '{codigo}' no corresponde a un valor válido de {typeof(TEnum).Name}.",
            nameof(codigo));
    }
}

