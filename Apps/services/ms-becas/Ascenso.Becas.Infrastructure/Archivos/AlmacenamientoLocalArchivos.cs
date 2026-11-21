using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Adaptador de archivos para desarrollo: escribe en un directorio local y
/// devuelve una URI <c>file://</c> o la URL base configurada.
/// </summary>
public sealed class AlmacenamientoLocalArchivos : IAlmacenamientoArchivos
{
    private readonly string _raiz;
    private readonly string? _urlBase;

    public AlmacenamientoLocalArchivos(OpcionesAlmacenamiento opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);

        var local = opciones.Local;

        if (string.IsNullOrWhiteSpace(local.RutaBase))
        {
            throw new InvalidOperationException(
                "La ruta base del almacenamiento local es obligatoria.");
        }

        _raiz = Path.GetFullPath(local.RutaBase.Trim());
        Directory.CreateDirectory(_raiz);

        if (!string.IsNullOrWhiteSpace(local.UrlBase))
        {
            if (!Uri.TryCreate(local.UrlBase.Trim(), UriKind.Absolute, out var url)
                || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    "La URL base del almacenamiento local debe ser una URL HTTP o HTTPS absoluta.");
            }

            _urlBase = local.UrlBase.Trim().TrimEnd('/');
        }
    }

    public async Task<ArchivoAlmacenado> SubirAsync(
        Stream contenido,
        string tipoMime,
        string rutaLogica,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contenido);
        ArgumentException.ThrowIfNullOrWhiteSpace(tipoMime);

        var ruta = RutasArchivo.Normalizar(rutaLogica);
        var destino = ResolverRuta(ruta);
        var directorio = Path.GetDirectoryName(destino)
            ?? throw new InvalidOperationException("No fue posible determinar el directorio del archivo.");

        Directory.CreateDirectory(directorio);

        var temporal = Path.Combine(
            directorio,
            $".{Path.GetFileName(destino)}.{Guid.NewGuid():N}.tmp");

        (string HashSha256, long TamanioBytes) huella;

        try
        {
            await using (var salida = new FileStream(
                temporal,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                HuellaArchivo.TamanioBuffer,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                huella = await HuellaArchivo.CopiarYCalcularAsync(
                    contenido,
                    salida,
                    cancellationToken);
                await salida.FlushAsync(cancellationToken);
            }

            File.Move(temporal, destino, overwrite: true);
        }
        catch
        {
            EliminarSiExiste(temporal);
            throw;
        }

        return new ArchivoAlmacenado(
            CrearUrl(ruta),
            huella.HashSha256,
            huella.TamanioBytes);
    }

    public Task EliminarAsync(string url, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ruta = ResolverUrl(url);

        if (File.Exists(ruta))
        {
            File.Delete(ruta);
        }

        return Task.CompletedTask;
    }

    private string CrearUrl(string ruta)
    {
        if (_urlBase is not null)
        {
            return $"{_urlBase}/{RutasArchivo.ABytesUrl(ruta)}";
        }

        return new Uri(ResolverRuta(ruta)).AbsoluteUri;
    }

    private string ResolverUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (_urlBase is not null
            && url.StartsWith(_urlBase + "/", StringComparison.OrdinalIgnoreCase))
        {
            var relativa = Uri.UnescapeDataString(url[(_urlBase.Length + 1)..]);
            return ResolverRuta(RutasArchivo.Normalizar(relativa));
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluta))
        {
            if (!absoluta.IsFile)
            {
                throw new ArgumentException(
                    "La URL no pertenece al almacenamiento local.",
                    nameof(url));
            }

            return ValidarDentroDeLaRaiz(Path.GetFullPath(absoluta.LocalPath));
        }

        return ResolverRuta(RutasArchivo.Normalizar(url));
    }

    private string ResolverRuta(string rutaNormalizada)
        => ValidarDentroDeLaRaiz(
            Path.GetFullPath(Path.Combine(_raiz, rutaNormalizada.Replace('/', Path.DirectorySeparatorChar))));

    private string ValidarDentroDeLaRaiz(string ruta)
    {
        var prefijo = _raiz.EndsWith(Path.DirectorySeparatorChar)
            ? _raiz
            : _raiz + Path.DirectorySeparatorChar;

        if (!ruta.StartsWith(prefijo, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "La ruta del archivo no pertenece al directorio de almacenamiento.",
                nameof(ruta));
        }

        return ruta;
    }

    private static void EliminarSiExiste(string ruta)
    {
        try
        {
            if (File.Exists(ruta))
            {
                File.Delete(ruta);
            }
        }
        catch (IOException)
        {
            // La compensación del archivo temporal es best-effort.
        }
        catch (UnauthorizedAccessException)
        {
            // La compensación del archivo temporal es best-effort.
        }
    }
}
