namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Unidad de trabajo transaccional del agregado.
/// </summary>
public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);

    Task EjecutarEnTransaccionAsync(
        Func<CancellationToken, Task> operacion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta la operación dentro de una transacción con el nivel de
    /// aislamiento indicado. Si ya existe una transacción activa se reutiliza
    /// para que los efectos y la idempotencia compartan el mismo commit.
    /// </summary>
    Task EjecutarEnTransaccionAsync(
        Func<CancellationToken, Task> operacion,
        NivelAislamientoTransaccion nivelAislamiento,
        CancellationToken cancellationToken = default);
}
