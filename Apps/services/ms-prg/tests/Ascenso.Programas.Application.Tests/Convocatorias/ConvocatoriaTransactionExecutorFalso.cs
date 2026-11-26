using Ascenso.Programas.Application.Convocatorias.Abstracciones;

namespace Ascenso.Programas.Application.Tests.Convocatorias;

internal sealed class ConvocatoriaTransactionExecutorFalso : IConvocatoriaTransactionExecutor
{
    public Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return operation(cancellationToken);
    }
}
