using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Unidad de trabajo simulada. Ejecuta la operación, permite observar el nivel
/// de aislamiento solicitado y separa la confirmación de la reversión.
/// </summary>
public sealed class UnidadDeTrabajoFalsa : IUnitOfWork
{
    public List<NivelAislamientoTransaccion> NivelesSolicitados { get; } = [];

    public int Confirmaciones { get; private set; }

    public int Reversiones { get; private set; }

    public int Guardados { get; private set; }

    public Exception? ErrorGuardado { get; set; }

    public Func<CancellationToken, Task>? AlConfirmar { get; set; }

    public Func<CancellationToken, Task>? AlRevertir { get; set; }

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ErrorGuardado is not null)
        {
            throw ErrorGuardado;
        }

        Guardados++;
        return Task.FromResult(1);
    }

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
        NivelesSolicitados.Add(nivelAislamiento);

        try
        {
            await operacion(cancellationToken);

            if (AlConfirmar is not null)
            {
                await AlConfirmar(cancellationToken);
            }

            Confirmaciones++;
        }
        catch
        {
            Reversiones++;

            if (AlRevertir is not null)
            {
                await AlRevertir(CancellationToken.None);
            }

            throw;
        }
    }
}
