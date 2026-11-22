using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Adaptador de archivos para Firebase/Google Cloud Storage. Calcula el hash y
/// el tamaño sobre los bytes que envía al bucket y devuelve una referencia
/// estable del objeto.
/// </summary>
public sealed class AlmacenamientoFirebaseArchivos : IAlmacenamientoArchivos
{
    private const string HostStorage = "storage.googleapis.com";
    private const string HostFirebase = "firebasestorage.googleapis.com";
    private static readonly TimeSpan _vigenciaUrlLectura = TimeSpan.FromMinutes(15);

    private readonly IAlmacenObjetosNube _objetos;
    private readonly string _bucket;
    private readonly string? _urlBase;
    private readonly bool _usaEmulador;

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
        _usaEmulador = firebase.UsaEmulador;

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

        if (_usaEmulador && _urlBase is null)
        {
            throw new InvalidOperationException(
                "La URL base de descarga es obligatoria cuando se usa Firebase Storage Emulator.");
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
            CrearReferencia(ruta),
            huella.HashSha256,
            huella.TamanioBytes);
    }

    public Task<string> ResolverUrlLecturaAsync(
        string referencia,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var objeto = ObtenerObjeto(referencia);

        if (_usaEmulador)
        {
            return Task.FromResult(CrearUrlEmulador(objeto));
        }

        return _objetos.CrearUrlFirmadaLecturaAsync(
            _bucket,
            objeto,
            _vigenciaUrlLectura,
            cancellationToken);
    }

    public async Task EliminarAsync(string url, CancellationToken cancellationToken = default)
    {
        var objeto = ObtenerObjeto(url);

        await _objetos.EliminarAsync(_bucket, objeto, cancellationToken);
    }

    private string CrearReferencia(string ruta)
        => $"gs://{_bucket}/{RutasArchivo.ABytesUrl(ruta)}";

    private string CrearUrlEmulador(string objeto)
    {
        if (_urlBase is null)
        {
            throw new InvalidOperationException(
                "La URL base de descarga es obligatoria cuando se usa Firebase Storage Emulator.");
        }

        return $"{_urlBase}/download/storage/v1/b/{Uri.EscapeDataString(_bucket)}/o/{Uri.EscapeDataString(objeto)}?alt=media";
    }

    private string ObtenerObjeto(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (Uri.TryCreate(url, UriKind.Absolute, out var referencia)
            && string.Equals(referencia.Scheme, "gs", StringComparison.OrdinalIgnoreCase))
        {
            ValidarBucket(referencia.Host);
            return RutasArchivo.Normalizar(
                Uri.UnescapeDataString(referencia.AbsolutePath.TrimStart('/')));
        }

        if (_usaEmulador)
        {
            return ObtenerObjetoEmulador(url);
        }

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

    private string ObtenerObjetoEmulador(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var absoluta)
            || _urlBase is null
            || !Uri.TryCreate(_urlBase, UriKind.Absolute, out var urlBase))
        {
            throw new ArgumentException(
                "La URL no pertenece al emulador de Firebase configurado.",
                nameof(url));
        }

        var prefijo = $"{urlBase.AbsolutePath.TrimEnd('/')}/download/storage/v1/b/{Uri.EscapeDataString(_bucket)}/o/";
        var ruta = absoluta.GetComponents(UriComponents.Path, UriFormat.UriEscaped);

        if (!string.Equals(absoluta.Scheme, urlBase.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(absoluta.Host, urlBase.Host, StringComparison.OrdinalIgnoreCase)
            || absoluta.Port != urlBase.Port
            || !ruta.StartsWith(prefijo.TrimStart('/'), StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "La URL no pertenece al bucket de Firebase configurado.",
                nameof(url));
        }

        var objeto = Uri.UnescapeDataString(ruta[prefijo.TrimStart('/').Length..]);
        return RutasArchivo.Normalizar(objeto);
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
