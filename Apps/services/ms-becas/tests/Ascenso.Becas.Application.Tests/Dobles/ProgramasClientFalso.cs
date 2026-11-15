using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Cliente de programas simulado para la reserva de cupo y presupuesto.
/// </summary>
public sealed class ProgramasClientFalso : IProgramasClient
{
    public ResultadoReservaPresupuesto? Resultado { get; set; }

    public Exception? Error { get; set; }

    public Guid? ProgramaSolicitado { get; private set; }

    public SolicitudReservaPresupuesto? UltimaSolicitud { get; private set; }

    public int Llamadas { get; private set; }

    public Task<ResultadoReservaPresupuesto> ReservarAsync(
        Guid programaBecaId,
        SolicitudReservaPresupuesto solicitud,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Llamadas++;
        ProgramaSolicitado = programaBecaId;
        UltimaSolicitud = solicitud;

        if (Error is not null)
        {
            throw Error;
        }

        return Task.FromResult(
            Resultado ?? throw new InvalidOperationException("La prueba no configuró el resultado de la reserva."));
    }
}
