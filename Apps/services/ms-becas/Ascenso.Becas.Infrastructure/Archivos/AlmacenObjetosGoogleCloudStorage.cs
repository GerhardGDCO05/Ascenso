using System.Net;
using Google;
using Google.Cloud.Storage.V1;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Implementación de <see cref="IAlmacenObjetosNube"/> sobre el cliente
/// oficial de Google Cloud Storage. Las operaciones son idempotentes en la
/// medida en que la API de Storage lo permite: borrar un objeto inexistente no
/// se considera un error.
/// </summary>
public sealed class AlmacenObjetosGoogleCloudStorage : IAlmacenObjetosNube
{
    private readonly StorageClient _cliente;

    public AlmacenObjetosGoogleCloudStorage(StorageClient cliente)
    {
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
    }

    public async Task SubirAsync(
        string bucket,
        string objeto,
        string tipoMime,
        Stream contenido,
        CancellationToken cancellationToken)
    {
        await _cliente.UploadObjectAsync(
            bucket,
            objeto,
            tipoMime,
            contenido,
            options: null,
            cancellationToken: cancellationToken);
    }

    public async Task EliminarAsync(
        string bucket,
        string objeto,
        CancellationToken cancellationToken)
    {
        try
        {
            await _cliente.DeleteObjectAsync(
                bucket,
                objeto,
                options: null,
                cancellationToken: cancellationToken);
        }
        catch (GoogleApiException excepcion) when (
            excepcion.HttpStatusCode == HttpStatusCode.NotFound)
        {
            // El archivo ya no existe: la eliminación es idempotente.
        }
    }

    public Task<string> CrearUrlFirmadaLecturaAsync(
        string bucket,
        string objeto,
        TimeSpan vigencia,
        CancellationToken cancellationToken)
        => _cliente.CreateUrlSigner().SignAsync(
            bucket,
            objeto,
            vigencia,
            HttpMethod.Get,
            SigningVersion.V4,
            cancellationToken);
}
