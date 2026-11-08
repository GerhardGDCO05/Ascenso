namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de almacenamiento de binarios. La implementación puede ser local
/// en desarrollo o Firebase/Google Cloud Storage en producción.
/// </summary>
public interface IAlmacenamientoArchivos
{
    Task<ArchivoAlmacenado> SubirAsync(
        Stream contenido,
        string tipoMime,
        string rutaLogica,
        CancellationToken cancellationToken = default);

    Task EliminarAsync(string url, CancellationToken cancellationToken = default);
}
