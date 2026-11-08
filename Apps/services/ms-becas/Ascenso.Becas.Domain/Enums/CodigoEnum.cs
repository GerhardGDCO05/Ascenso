using System.Text;

namespace Ascenso.Becas.Domain.Enums;

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
}
