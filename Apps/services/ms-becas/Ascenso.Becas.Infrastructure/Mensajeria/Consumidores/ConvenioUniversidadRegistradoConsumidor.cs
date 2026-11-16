using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Ascenso.Becas.Infrastructure.Persistence.Proyecciones;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.Infrastructure.Mensajeria.Consumidores;

/// <summary>
/// Aplica <c>ConvenioUniversidadRegistrado</c> a la proyección local con upsert
/// idempotente.
/// </summary>
public sealed partial class ConvenioUniversidadRegistradoConsumidor
    : IConsumer<EventoIntegracion<ConvenioUniversidadRegistrado>>
{
    private readonly ActualizadorProyeccionesRef _actualizador;
    private readonly ILogger<ConvenioUniversidadRegistradoConsumidor> _logger;

    public ConvenioUniversidadRegistradoConsumidor(
        ActualizadorProyeccionesRef actualizador,
        ILogger<ConvenioUniversidadRegistradoConsumidor> logger)
    {
        _actualizador = actualizador ?? throw new ArgumentNullException(nameof(actualizador));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(
        ConsumeContext<EventoIntegracion<ConvenioUniversidadRegistrado>> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var mensaje = context.Message;
        var payload = mensaje.Payload
            ?? throw new InvalidOperationException("El evento no contiene el payload de convenio.");

        CodigosIntegracion.ValidarEvento(
            mensaje.EventId,
            mensaje.AggregateId,
            nameof(ConvenioUniversidadRegistrado));

        if (payload.UniversidadAliadaId == Guid.Empty
            || payload.ConvenioId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.Estado))
        {
            throw new InvalidOperationException(
                "El evento ConvenioUniversidadRegistrado está incompleto o es inválido.");
        }

        var modelo = new ConvenioRef
        {
            UniversidadAliadaId = payload.UniversidadAliadaId,
            ConvenioId = payload.ConvenioId,
            Estado = payload.Estado.Trim(),
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
        EventId = 7102,
        Level = LogLevel.Information,
        Message = "ConvenioUniversidadRegistrado {EventId} aplicado sobre {UniversidadAliadaId}: {Filas} fila(s).")]
    private static partial void RegistrarAplicacion(
        ILogger logger,
        Guid eventId,
        Guid universidadAliadaId,
        int filas);
}
