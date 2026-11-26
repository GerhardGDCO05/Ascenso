namespace Ascenso.Programas.Application.Convocatorias.Abstracciones;

/// <summary>
/// Ejecuta una operación de convocatoria dentro de una transacción local.
/// </summary>
public interface IConvocatoriaTransactionExecutor
{
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
