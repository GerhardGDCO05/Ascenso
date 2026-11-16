using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Ascenso.Becas.Infrastructure.Persistence.Proyecciones;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.Infrastructure.Mensajeria.Consumidores;

/// <summary>
/// Aplica <c>ConvenioUniversidadVencido</c> a la proyección local. Si el evento
/// llega fuera de orden, el control de <c>occurredAt</c> evita retroceder un
/// estado más reciente.
/// </summary>
public sealed partial class ConvenioUniversidadVencidoConsumidor
    : IConsumer<EventoIntegracion<ConvenioUniversidadVencido>>
{
    private readonly ActualizadorProyeccionesRef _actualizador;
    private readonly ILogger<ConvenioUniversidadVencidoConsumidor> _logger;

    public ConvenioUniversidadVencidoConsumidor(
        ActualizadorProyeccionesRef actualizador,
        ILogger<ConvenioUniversidadVencidoConsumidor> logger)
    {
        _actualizador = actualizador ?? throw new ArgumentNullException(nameof(actualizador));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(
        ConsumeContext<EventoIntegracion<ConvenioUniversidadVencido>> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var mensaje = context.Message;
        var payload = mensaje.Payload
            ?? throw new InvalidOperationException("El evento no contiene el payload de convenio vencido.");

        CodigosIntegracion.ValidarEvento(
            mensaje.EventId,
            mensaje.AggregateId,
            nameof(ConvenioUniversidadVencido));

        if (payload.UniversidadAliadaId == Guid.Empty
            || payload.ConvenioId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "El evento ConvenioUniversidadVencido está incompleto o es inválido.");
        }

        var modelo = new ConvenioRef
        {
            UniversidadAliadaId = payload.UniversidadAliadaId,
            ConvenioId = payload.ConvenioId,
            Estado = string.IsNullOrWhiteSpace(payload.Estado)
                ? "VENCIDO"
                : payload.Estado.Trim(),
            FechaFin = payload.FechaFin,
            UltimoEventoId = mensaje.EventId,
            OccurredAt = mensaje.OccurredAt
        };

        var filas = await _actualizador.ActualizarConvenioAsync(
            modelo,
            context.CancellationToken);

        RegistrarAplicacion(
            _logger,
            mensaje.EventId,
            payload.UniversidadAliadaId,
            filas);
    }

    [LoggerMessage(
        EventId = 7103,
        Level = LogLevel.Information,
        Message = "ConvenioUniversidadVencido {EventId} aplicado sobre {UniversidadAliadaId}: {Filas} fila(s).")]
    private static partial void RegistrarAplicacion(
        ILogger logger,
        Guid eventId,
        Guid universidadAliadaId,
        int filas);
}
