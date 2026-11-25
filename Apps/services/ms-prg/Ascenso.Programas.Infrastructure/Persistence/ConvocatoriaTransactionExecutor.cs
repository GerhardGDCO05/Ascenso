using Ascenso.Programas.Application.Convocatorias.Abstracciones;

namespace Ascenso.Programas.Infrastructure.Persistence;

/// <summary>
/// Mantiene la transacción local abierta durante la validación del programa y el guardado.
/// </summary>
public sealed class ConvocatoriaTransactionExecutor : IConvocatoriaTransactionExecutor
{
    private readonly ProgramasDbContext _contexto;

    public ConvocatoriaTransactionExecutor(ProgramasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_contexto.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("La operación de convocatoria ya tiene una transacción activa.");
        }

        await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);
        var resultado = await operation(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);

        return resultado;
    }
}
