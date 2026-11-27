using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Cliente de programas simulado para la reserva de cupo y presupuesto.
/// </summary>
public sealed class ProgramasClientFalso : IProgramasClient
{
    public ResultadoReservaPresupuesto? Resultado { get; set; }

    public Func<Guid, Guid, Guid, Guid, ResultadoCondicionesPrograma?>? CrearCondiciones { get; set; }

    public Exception? ErrorCondiciones { get; set; }

    public Exception? Error { get; set; }

    public Exception? ErrorLiberar { get; set; }

    public Exception? ErrorConfirmar { get; set; }
    public Exception? ErrorActivar { get; set; }
    public List<(Guid ProgramaId, Guid ReservaId, Guid ReferenciaId)> Activaciones { get; } = [];

    public List<(Guid ProgramaId, Guid ReservaId, Guid ReferenciaId)> Confirmaciones { get; } = [];

    public List<(Guid ProgramaId, Guid ReservaId, Guid ReferenciaId)> Liberaciones { get; } = [];

    public Guid? ProgramaSolicitado { get; private set; }

    public SolicitudReservaPresupuesto? UltimaSolicitud { get; private set; }

    public int Llamadas { get; private set; }

    public int ConsultasCondiciones { get; private set; }

    public Task<ResultadoCondicionesPrograma?> ObtenerCondicionesAsync(
        Guid programaBecaId,
        Guid carreraId,
        Guid periodoAcademicoId,
        Guid universidadAliadaId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConsultasCondiciones++;

        if (ErrorCondiciones is not null)
        {
            throw ErrorCondiciones;
        }

        return Task.FromResult(
            CrearCondiciones?.Invoke(
                programaBecaId,
                carreraId,
                periodoAcademicoId,
                universidadAliadaId));
    }

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

    public Task ConfirmarReservaAsync(
        Guid programaBecaId, Guid reservaId, Guid referenciaId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ErrorConfirmar is not null)
            throw ErrorConfirmar;
        Confirmaciones.Add((programaBecaId, reservaId, ofertaId));
        return Task.CompletedTask;
    }

    public Task ActivarReservaAsync(
        Guid programaBecaId, Guid reservaId, Guid referenciaId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ErrorActivar is not null)
            throw ErrorActivar;
        Activaciones.Add((programaBecaId, reservaId, ofertaId));
        return Task.CompletedTask;
    }

    public Task LiberarReservaAsync(
        Guid programaBecaId, Guid reservaId, Guid referenciaId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ErrorLiberar is not null)
            throw ErrorLiberar;
        Liberaciones.Add((programaBecaId, reservaId, ofertaId));
        return Task.CompletedTask;
    }
}
