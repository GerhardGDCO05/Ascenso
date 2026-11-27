using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Otorgamiento.Servicios;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Observabilidad;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>Repite la coordinación de emisiones preparadas con la misma oferta.</summary>
public sealed class ReconciliarEmisionesOfertaJob
{
    private static readonly Action<ILogger, Guid, Exception?> _pendiente =
        LoggerMessage.Define<Guid>(LogLevel.Warning,
            new EventId(1, nameof(ReconciliarEmisionesOfertaJob)),
            "La emisión {OfertaId} permanece pendiente de coordinación.");
    private readonly IIntencionEmisionOfertaStore _intenciones;
    private readonly FinalizadorEmisionOferta _finalizador;
    private readonly IOptions<OpcionesTrabajosHangfire> _opciones;
    private readonly ILogger<ReconciliarEmisionesOfertaJob> _logger;

    public ReconciliarEmisionesOfertaJob(IIntencionEmisionOfertaStore intenciones,
        FinalizadorEmisionOferta finalizador,
        IOptions<OpcionesTrabajosHangfire> opciones,
        ILogger<ReconciliarEmisionesOfertaJob> logger)
    {
        _intenciones = intenciones ?? throw new ArgumentNullException(nameof(intenciones));
        _finalizador = finalizador ?? throw new ArgumentNullException(nameof(finalizador));
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Queue(RegistroHangfire.ColaCritica)]
    public Task Ejecutar(CancellationToken cancellationToken = default)
        => TelemetriaHangfire.EjecutarAsync(nameof(ReconciliarEmisionesOfertaJob),
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
                    DependenciaNoDisponibleException or PostulacionOtorgamientoRechazadaException)
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
