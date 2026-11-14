using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Net.Http.Headers;

namespace Ascenso.Becas.Api.Idempotencia;

/// <summary>
/// Calcula el hash SHA-256 del payload canónico. Para JSON ordena las
/// propiedades de forma ordinal y elimina espacios; para otros contenidos usa
/// los bytes originales.
/// </summary>
public static class HashPayload
{
    public static async Task<string> CalcularAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.EnableBuffering();
        request.Body.Position = 0;

        try
        {
            using var memoria = new MemoryStream();
            await request.Body.CopyToAsync(memoria, cancellationToken);

            var canonico = Canonicalizar(
                memoria.ToArray(),
                request.ContentType);

            return Convert.ToHexString(SHA256.HashData(canonico))
                .ToLowerInvariant();
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    private static byte[] Canonicalizar(byte[] contenido, string? contentType)
    {
        if (contenido.Length == 0)
        {
            return contenido;
        }

        try
        {
            using var documento = JsonDocument.Parse(contenido);
            using var salida = new MemoryStream();

            using (var escritor = new Utf8JsonWriter(salida))
            {
                Escribir(documento.RootElement, escritor);
            }

            return salida.ToArray();
        }
        catch (JsonException)
        {
            return NormalizarMultipart(contenido, contentType);
        }
    }

    private static byte[] NormalizarMultipart(byte[] contenido, string? contentType)
    {
        if (!MediaTypeHeaderValue.TryParse(contentType, out var tipo)
            || !tipo.MediaType.Equals(
                "multipart/form-data",
                StringComparison.OrdinalIgnoreCase)
            || !tipo.Boundary.HasValue)
        {
            return contenido;
        }

        // El boundary lo genera el cliente en cada envío; se reemplaza por un
        // valor fijo para que el mismo formulario produzca el mismo hash.
        var texto = Encoding.Latin1.GetString(contenido);
        var normalizado = texto.Replace(
            $"--{tipo.Boundary.Value}",
            "--limite-idempotente",
            StringComparison.Ordinal);

        return Encoding.Latin1.GetBytes(normalizado);
    }

    private static void Escribir(JsonElement elemento, Utf8JsonWriter escritor)
    {
        switch (elemento.ValueKind)
        {
            case JsonValueKind.Object:
                escritor.WriteStartObject();

                foreach (var propiedad in elemento.EnumerateObject()
                             .OrderBy(propiedad => propiedad.Name, StringComparer.Ordinal))
                {
                    escritor.WritePropertyName(propiedad.Name);
                    Escribir(propiedad.Value, escritor);
                }

                escritor.WriteEndObject();
                break;

            case JsonValueKind.Array:
                escritor.WriteStartArray();

                foreach (var item in elemento.EnumerateArray())
                {
                    Escribir(item, escritor);
                }

                escritor.WriteEndArray();
                break;

            case JsonValueKind.String:
                escritor.WriteStringValue(elemento.GetString());
                break;

            case JsonValueKind.Number:
                escritor.WriteRawValue(elemento.GetRawText());
                break;

            case JsonValueKind.True:
                escritor.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                escritor.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                escritor.WriteNullValue();
                break;

            default:
                escritor.WriteRawValue(elemento.GetRawText());
                break;
        }
    }
}
