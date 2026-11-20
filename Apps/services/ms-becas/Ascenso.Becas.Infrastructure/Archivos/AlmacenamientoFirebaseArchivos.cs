using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Adaptador de archivos para Firebase/Google Cloud Storage. Calcula el hash y
/// el tamaño sobre los bytes que envía al bucket y devuelve la URL canónica del
/// objeto.
/// </summary>
public sealed class AlmacenamientoFirebaseArchivos : IAlmacenamientoArchivos
{
    private const string HostStorage = "storage.googleapis.com";
    private const string HostFirebase = "firebasestorage.googleapis.com";

    private readonly IAlmacenObjetosNube _objetos;
    private readonly string _bucket;
    private readonly string? _urlBase;

    public AlmacenamientoFirebaseArchivos(
        IAlmacenObjetosNube objetos,
        OpcionesAlmacenamiento opciones)
    {
        _objetos = objetos ?? throw new ArgumentNullException(nameof(objetos));
        ArgumentNullException.ThrowIfNull(opciones);

        var firebase = opciones.Firebase;

        if (string.IsNullOrWhiteSpace(firebase.Bucket))
        {
            throw new InvalidOperationException(
                "El bucket de Firebase es obligatorio cuando el proveedor es Firebase.");
        }

        _bucket = firebase.Bucket.Trim();

        if (!string.IsNullOrWhiteSpace(firebase.UrlBase))
        {
            if (!Uri.TryCreate(firebase.UrlBase.Trim(), UriKind.Absolute, out var url)
                || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    "La URL base de Firebase debe ser una URL HTTP o HTTPS absoluta.");
            }

            _urlBase = firebase.UrlBase.Trim().TrimEnd('/');
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

        using var memoria = new MemoryStream();
        var huella = await HuellaArchivo.CopiarYCalcularAsync(
            contenido,
            memoria,
            cancellationToken);

        memoria.Position = 0;

        await _objetos.SubirAsync(
            _bucket,
            ruta,
            tipoMime,
            memoria,
            cancellationToken);

        return new ArchivoAlmacenado(
            CrearUrl(ruta),
            huella.HashSha256,
            huella.TamanioBytes);
    }

    public async Task EliminarAsync(string url, CancellationToken cancellationToken = default)
    {
        var objeto = ObtenerObjeto(url);

        await _objetos.EliminarAsync(_bucket, objeto, cancellationToken);
    }

    private string CrearUrl(string ruta)
    {
        if (_urlBase is not null)
        {
            return $"{_urlBase}/{RutasArchivo.ABytesUrl(ruta)}";
        }

        return $"https://{HostStorage}/{Uri.EscapeDataString(_bucket)}/{RutasArchivo.ABytesUrl(ruta)}";
    }

    private string ObtenerObjeto(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (_urlBase is not null
            && url.StartsWith(_urlBase + "/", StringComparison.OrdinalIgnoreCase))
        {
            return RutasArchivo.Normalizar(
                Uri.UnescapeDataString(url[(_urlBase.Length + 1)..]));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var absoluta))
        {
            return RutasArchivo.Normalizar(url);
        }

        if (string.Equals(absoluta.Scheme, "gs", StringComparison.OrdinalIgnoreCase))
        {
            ValidarBucket(absoluta.Host);
            return RutasArchivo.Normalizar(
                Uri.UnescapeDataString(absoluta.AbsolutePath.TrimStart('/')));
        }

        if (string.Equals(absoluta.Host, HostStorage, StringComparison.OrdinalIgnoreCase))
        {
            var partes = absoluta.AbsolutePath
                .TrimStart('/')
                .Split('/', 2, StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length == 2)
            {
                ValidarBucket(Uri.UnescapeDataString(partes[0]));
                return RutasArchivo.Normalizar(
                    Uri.UnescapeDataString(partes[1]));
            }
        }

        if (string.Equals(absoluta.Host, HostFirebase, StringComparison.OrdinalIgnoreCase))
        {
            var objeto = ObtenerObjetoFirebase(absoluta);
            return RutasArchivo.Normalizar(Uri.UnescapeDataString(objeto));
        }

        throw new ArgumentException(
            "La URL no pertenece al bucket de Firebase configurado.",
            nameof(url));
    }

    private string ObtenerObjetoFirebase(Uri url)
    {
        // Formato: /v0/b/{bucket}/o/{objeto}
        var partes = url.AbsolutePath
            .TrimStart('/')
            .Split('/', 5, StringSplitOptions.RemoveEmptyEntries);

        if (partes.Length != 5
            || !string.Equals(partes[0], "v0", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(partes[1], "b", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(partes[3], "o", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "La URL de Firebase no tiene el formato esperado.",
                nameof(url));
        }

        ValidarBucket(Uri.UnescapeDataString(partes[2]));
        return partes[4];
    }

    private void ValidarBucket(string bucket)
    {
        if (!string.Equals(bucket, _bucket, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "La URL no pertenece al bucket de Firebase configurado.",
                nameof(bucket));
        }
    }
}
