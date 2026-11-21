namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Abstracción mínima sobre el cliente de objetos de Google Cloud Storage para
/// poder probar el adaptador sin credenciales ni red.
/// </summary>
public interface IAlmacenObjetosNube
{
    Task SubirAsync(
        string bucket,
        string objeto,
        string tipoMime,
        Stream contenido,
        CancellationToken cancellationToken);

    Task EliminarAsync(
        string bucket,
        string objeto,
        CancellationToken cancellationToken);
}
