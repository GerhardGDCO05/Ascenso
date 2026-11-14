namespace Ascenso.Becas.Application.Idempotencia;

/// <summary>
/// Almacén transaccional de solicitudes idempotentes. La transacción agrupa el
/// registro de la clave con los efectos del caso de uso.
/// </summary>
public interface IAlmacenIdempotencia
{
    Task<RegistroIdempotencia?> ObtenerAsync(
        string endpoint,
        string clave,
        CancellationToken cancellationToken = default);

    Task IniciarTransaccionAsync(CancellationToken cancellationToken = default);

    Task<bool> IntentarRegistrarAsync(
        RegistroIdempotencia registro,
        CancellationToken cancellationToken = default);

    Task CompletarAsync(
        string endpoint,
        string clave,
        int statusCode,
        string? responseBody,
        Guid? recursoId,
        CancellationToken cancellationToken = default);

    Task EliminarAsync(
        string endpoint,
        string clave,
        CancellationToken cancellationToken = default);

    Task ConfirmarAsync(CancellationToken cancellationToken = default);

    Task RevertirAsync(CancellationToken cancellationToken = default);
}
