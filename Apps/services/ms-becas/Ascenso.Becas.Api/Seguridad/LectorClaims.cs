using System.Security.Claims;
using System.Text.Json;

namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Lectura tolerante de claims que pueden venir como valor único, lista
/// separada por espacios o arreglo JSON.
/// </summary>
public static class LectorClaims
{
    public static IReadOnlyList<string> Valores(ClaimsPrincipal usuario, string tipoClaim)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(tipoClaim);

        return usuario.FindAll(tipoClaim)
            .SelectMany(Parsear)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string? PrimerValor(ClaimsPrincipal usuario, params string[] tiposClaim)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(tiposClaim);

        foreach (var tipo in tiposClaim)
        {
            var valor = usuario.FindFirst(tipo)?.Value;
            if (!string.IsNullOrWhiteSpace(valor))
            {
                return valor.Trim();
            }
        }

        return null;
    }

    private static IEnumerable<string> Parsear(Claim claim)
    {
        var valor = claim.Value.Trim();

        if (string.IsNullOrWhiteSpace(valor))
        {
            return [];
        }

        if (valor.StartsWith('['))
        {
            try
            {
                var elementos = JsonSerializer.Deserialize<string[]>(valor);

                if (elementos is not null)
                {
                    return elementos
                        .Where(elemento => !string.IsNullOrWhiteSpace(elemento))
                        .Select(elemento => elemento.Trim())
                        .ToArray();
                }
            }
            catch (JsonException)
            {
                // No es un arreglo JSON válido: se interpreta como lista plana.
            }
        }

        return valor.Split(
            [' ', ','],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
