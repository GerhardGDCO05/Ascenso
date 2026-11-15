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

        if (ErrorSubida is not null)
        {
            throw ErrorSubida;
        }

        RutasSubidas.Add(rutaLogica);

        if (!CalcularHash)
        {
            return Resultado;
        }

        var hash = await SHA256.HashDataAsync(contenido, cancellationToken);

        return Resultado with
        {
            HashSha256 = Convert.ToHexString(hash).ToLowerInvariant()
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
