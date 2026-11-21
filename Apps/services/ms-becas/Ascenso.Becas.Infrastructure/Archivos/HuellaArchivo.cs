using System.Security.Cryptography;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Utilidades compartidas por los adaptadores de almacenamiento para calcular
/// el SHA-256 y el tamaño de los bytes que realmente se escriben.
/// </summary>
internal static class HuellaArchivo
{
    internal const int TamanioBuffer = 81920;

    public static async Task<(string HashSha256, long TamanioBytes)> CopiarYCalcularAsync(
        Stream origen,
        Stream destino,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(origen);
        ArgumentNullException.ThrowIfNull(destino);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[TamanioBuffer];
        long tamanio = 0;

        int leidos;
        while ((leidos = await origen.ReadAsync(
                buffer.AsMemory(0, buffer.Length),
                cancellationToken)) > 0)
        {
            await destino.WriteAsync(
                buffer.AsMemory(0, leidos),
                cancellationToken);
            hash.AppendData(buffer, 0, leidos);
            tamanio += leidos;
        }

        return (Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(), tamanio);
    }
}

/// <summary>
/// Normaliza las rutas lógicas para que no puedan escapar del directorio raíz.
/// </summary>
internal static class RutasArchivo
{
    public static string Normalizar(string rutaLogica)
    {
        if (string.IsNullOrWhiteSpace(rutaLogica))
        {
            throw new ArgumentException("La ruta lógica del archivo es obligatoria.", nameof(rutaLogica));
        }

        if (Path.IsPathRooted(rutaLogica))
        {
            throw new ArgumentException("La ruta lógica debe ser relativa.", nameof(rutaLogica));
        }

        var segmentos = rutaLogica
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segmentos.Length == 0
            || segmentos.Any(segmento => segmento is "." or ".."))
        {
            throw new ArgumentException(
                "La ruta lógica contiene segmentos no permitidos.",
                nameof(rutaLogica));
        }

        return string.Join('/', segmentos);
    }

    public static string ABytesUrl(string rutaNormalizada)
        => string.Join(
            '/',
            rutaNormalizada.Split('/').Select(Uri.EscapeDataString));
}
