using System.Data;
using Ascenso.Becas.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;

/// <summary>
/// Unidad de trabajo EF Core. Reutiliza la transacción ambiental, por ejemplo
/// la abierta por el middleware de idempotencia, para que la fila idempotente y
/// el cambio de estado compartan el mismo commit.
/// </summary>
public sealed class UnidadDeTrabajo : IUnitOfWork
{
    private readonly BecasDbContext _contexto;

    public UnidadDeTrabajo(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        => _contexto.SaveChangesAsync(cancellationToken);

    public Task EjecutarEnTransaccionAsync(
        Func<CancellationToken, Task> operacion,
        CancellationToken cancellationToken = default)
        => EjecutarEnTransaccionAsync(
            operacion,
            NivelAislamientoTransaccion.ReadCommitted,
            cancellationToken);

    public async Task EjecutarEnTransaccionAsync(
        Func<CancellationToken, Task> operacion,
        NivelAislamientoTransaccion nivelAislamiento,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        if (_contexto.Database.CurrentTransaction is not null)
        {
            await operacion(cancellationToken);
            return;
        }

        var nivel = nivelAislamiento == NivelAislamientoTransaccion.Serializable
            ? IsolationLevel.Serializable
            : IsolationLevel.ReadCommitted;

        await using var transaccion = await _contexto.Database.BeginTransactionAsync(
            nivel,
            cancellationToken);

        try
        {
            await operacion(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await transaccion.RollbackAsync(CancellationToken.None);
            }
            catch
            {
                // Se preserva la excepción original de la operación.
            }

            throw;
        }
    }
}
