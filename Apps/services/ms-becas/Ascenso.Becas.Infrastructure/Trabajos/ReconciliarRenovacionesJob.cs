using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Renovacion.Comandos.ExpirarSolicitudRenovacion;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Observabilidad;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>Reintenta de forma acotada las solicitudes vencidas sin decisión.</summary>
public sealed class ReconciliarRenovacionesJob
{
    private static readonly Action<ILogger, Guid, Exception?> _configuracionPendiente =
        LoggerMessage.Define<Guid>(LogLevel.Warning,
            new EventId(1, nameof(ReconciliarRenovacionesJob)),
            "Renovación {SolicitudId} pendiente por configuración oficial.");
    private static readonly Action<ILogger, Guid, Exception?> _integracionPendiente =
        LoggerMessage.Define<Guid>(LogLevel.Warning,
            new EventId(2, nameof(ReconciliarRenovacionesJob)),
            "Renovación {SolicitudId} pendiente por integración externa.");
    private readonly ISolicitudRenovacionRepository _solicitudes;
    private readonly ICommandDispatcher _despachador;
    private readonly IClock _reloj;
    private readonly IOptions<OpcionesTrabajosHangfire> _opciones;
    private readonly ILogger<ReconciliarRenovacionesJob> _logger;

    public ReconciliarRenovacionesJob(
        ISolicitudRenovacionRepository solicitudes,
        ICommandDispatcher despachador, IClock reloj,
        IOptions<OpcionesTrabajosHangfire> opciones,
        ILogger<ReconciliarRenovacionesJob> logger)
    {
        _solicitudes = solicitudes ?? throw new ArgumentNullException(nameof(solicitudes));
        _despachador = despachador ?? throw new ArgumentNullException(nameof(despachador));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Queue(RegistroHangfire.ColaCritica)]
    public Task Ejecutar(CancellationToken cancellationToken = default)
        => TelemetriaHangfire.EjecutarAsync(
            nameof(ReconciliarRenovacionesJob),
            () => EjecutarAsync(cancellationToken));

    private async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(_reloj.UtcNow.UtcDateTime);
        var limite = Math.Max(1,
            _opciones.Value.LimiteRenovacionesPorReconciliacion);
        DateOnly? ultimaFecha = null;
        Guid? ultimoId = null;
        while (true)
        {
            var vencidas = await _solicitudes.ObtenerEnCursoVencidasAsync(
                hoy, limite, ultimaFecha, ultimoId, cancellationToken);
            if (vencidas.Count == 0)
                break;
            foreach (var solicitud in vencidas)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await _despachador.EnviarAsync<ExpirarSolicitudRenovacionCommand, bool>(
                        new ExpirarSolicitudRenovacionCommand(solicitud.Id),
                        cancellationToken);
                }
                catch (ConfiguracionProgramaNoDisponibleException excepcion)
                {
                    _configuracionPendiente(_logger, solicitud.Id, excepcion);
                }
                catch (DependenciaNoDisponibleException excepcion)
                {
                    _integracionPendiente(_logger, solicitud.Id, excepcion);
                }
                catch (HttpRequestException excepcion)
                {
                    _integracionPendiente(_logger, solicitud.Id, excepcion);
                }
            }
            ultimaFecha = vencidas[^1].FechaLimite;
            ultimoId = vencidas[^1].Id;
            if (vencidas.Count < limite)
                break;
        }
    }
}
