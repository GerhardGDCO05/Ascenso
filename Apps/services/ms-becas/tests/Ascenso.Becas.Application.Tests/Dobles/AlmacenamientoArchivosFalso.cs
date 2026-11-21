using System.Security.Cryptography;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Almacenamiento de archivos simulado que registra las rutas subidas y
/// eliminadas. Por defecto devuelve el hash real del contenido recibido, como
/// haría un almacenamiento real; puede desactivarse para simular discrepancias.
/// </summary>
public sealed class AlmacenamientoArchivosFalso : IAlmacenamientoArchivos
{
    public ArchivoAlmacenado Resultado { get; set; } = new(
        "https://storage.example/becas/archivo.pdf",
        new string('a', 64),
        64);

    public bool CalcularHash { get; set; } = true;

    public Exception? ErrorSubida { get; set; }

    public Exception? ErrorEliminacion { get; set; }

    public Dictionary<string, ArchivoAlmacenado> ResultadosPorRuta { get; } = [];

    public Dictionary<string, Exception> ErroresSubidaPorRuta { get; } = [];

    public Func<string, ArchivoAlmacenado>? CrearResultado { get; set; }

    public Func<string, Exception?>? ErrorSubidaSegunRuta { get; set; }

    public List<string> RutasSubidas { get; } = [];

    public List<string> UrlsEliminadas { get; } = [];

    public async Task<ArchivoAlmacenado> SubirAsync(
        Stream contenido,
        string tipoMime,
        string rutaLogica,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(contenido);

        var errorSegunRuta = ErrorSubidaSegunRuta?.Invoke(rutaLogica);
        if (errorSegunRuta is not null)
        {
            throw errorSegunRuta;
        }

        if (ErroresSubidaPorRuta.TryGetValue(rutaLogica, out var errorPorRuta))
        {
            throw errorPorRuta;
        }

        if (ErrorSubida is not null)
        {
            throw ErrorSubida;
        }

        RutasSubidas.Add(rutaLogica);

        var resultado = CrearResultado?.Invoke(rutaLogica)
            ?? (ResultadosPorRuta.TryGetValue(rutaLogica, out var configurado)
                ? configurado
                : Resultado);

        if (!CalcularHash)
        {
            return resultado;
        }

        var tamanio = contenido.CanSeek
            ? contenido.Length
            : resultado.TamanioBytes;
        var hash = await SHA256.HashDataAsync(contenido, cancellationToken);

        return resultado with
        {
            HashSha256 = Convert.ToHexString(hash).ToLowerInvariant(),
            TamanioBytes = tamanio
        };
    }

    public Task EliminarAsync(string url, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ErrorEliminacion is not null)
        {
            throw ErrorEliminacion;
        }

        UrlsEliminadas.Add(url);
        return Task.CompletedTask;
    }
}
