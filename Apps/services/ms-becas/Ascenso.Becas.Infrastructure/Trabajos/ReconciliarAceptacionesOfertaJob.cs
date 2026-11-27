using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Otorgamiento.Servicios;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Observabilidad;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>Reintenta aceptaciones con reserva activada o activación incierta.</summary>
public sealed class ReconciliarAceptacionesOfertaJob
{
    private static readonly Action<ILogger, Guid, Exception?> _pendiente =
        LoggerMessage.Define<Guid>(LogLevel.Warning,
            new EventId(1, nameof(ReconciliarAceptacionesOfertaJob)),
            "La aceptación {OfertaId} permanece pendiente de coordinación.");
    private readonly IIntencionAceptacionOfertaStore _intenciones;
    private readonly FinalizadorAceptacionOferta _finalizador;
    private readonly IOptions<OpcionesTrabajosHangfire> _opciones;
    private readonly ILogger<ReconciliarAceptacionesOfertaJob> _logger;

    public ReconciliarAceptacionesOfertaJob(IIntencionAceptacionOfertaStore intenciones,
        FinalizadorAceptacionOferta finalizador,
        IOptions<OpcionesTrabajosHangfire> opciones,
        ILogger<ReconciliarAceptacionesOfertaJob> logger)
    {
        _intenciones = intenciones ?? throw new ArgumentNullException(nameof(intenciones));
        _finalizador = finalizador ?? throw new ArgumentNullException(nameof(finalizador));
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Queue(RegistroHangfire.ColaCritica)]
    public Task Ejecutar(CancellationToken cancellationToken = default)
        => TelemetriaHangfire.EjecutarAsync(nameof(ReconciliarAceptacionesOfertaJob),
            () => EjecutarAsync(cancellationToken));

    private async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        var limite = Math.Max(1, _opciones.Value.LimiteOfertasPorReconciliacion);
        Guid? despuesDe = null;
        while (true)
        {
            var ids = await _intenciones.ListarPendientesAsync(limite,
                despuesDe, cancellationToken);
            if (ids.Count == 0)
                break;
            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var intencion = await _intenciones.ObtenerAsync(id, cancellationToken);
                if (intencion is null)
                    continue;
                try
                {
                    await _finalizador.ConfirmarAsync(intencion, cancellationToken);
                }
                catch (Exception excepcion) when (excepcion is
                    DependenciaNoDisponibleException or ConflictoOperacionException
                    or ConfiguracionProgramaNoDisponibleException)
                {
                    _pendiente(_logger, id, excepcion);
                }
            }
            despuesDe = ids[^1];
            if (ids.Count < limite)
                break;
        }
    }
}
