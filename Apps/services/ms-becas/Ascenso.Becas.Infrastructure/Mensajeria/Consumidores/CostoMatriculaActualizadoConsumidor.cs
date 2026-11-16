using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Ascenso.Becas.Infrastructure.Persistence.Proyecciones;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.Infrastructure.Mensajeria.Consumidores;

/// <summary>
/// Aplica <c>CostoMatriculaActualizado</c> a la proyección local con upsert
/// idempotente y control de versión por <c>occurredAt</c>.
/// </summary>
public sealed partial class CostoMatriculaActualizadoConsumidor
    : IConsumer<EventoIntegracion<CostoMatriculaActualizado>>
{
    private readonly ActualizadorProyeccionesRef _actualizador;
    private readonly ILogger<CostoMatriculaActualizadoConsumidor> _logger;

    public CostoMatriculaActualizadoConsumidor(
        ActualizadorProyeccionesRef actualizador,
        ILogger<CostoMatriculaActualizadoConsumidor> logger)
    {
        _actualizador = actualizador ?? throw new ArgumentNullException(nameof(actualizador));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(
        ConsumeContext<EventoIntegracion<CostoMatriculaActualizado>> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var mensaje = context.Message;
        var payload = mensaje.Payload
            ?? throw new InvalidOperationException("El evento no contiene el payload de costo de matrícula.");

        CodigosIntegracion.ValidarEvento(
            mensaje.EventId,
            mensaje.AggregateId,
            nameof(CostoMatriculaActualizado));

        if (payload.CostoMatriculaReferenciaId == Guid.Empty
            || payload.CarreraId == Guid.Empty
            || payload.PeriodoAcademicoId == Guid.Empty
            || payload.MontoReferenciaMatricula < 0
            || payload.NumeroVersion <= 0
            || string.IsNullOrWhiteSpace(payload.Moneda)
            || string.IsNullOrWhiteSpace(payload.Estado))
        {
            throw new InvalidOperationException(
                "El evento CostoMatriculaActualizado está incompleto o es inválido.");
        }

        var modelo = new CostoMatriculaRef
        {
            CostoMatriculaReferenciaId = payload.CostoMatriculaReferenciaId,
            CarreraId = payload.CarreraId,
            PeriodoAcademicoId = payload.PeriodoAcademicoId,
            MontoReferenciaMatricula = payload.MontoReferenciaMatricula,
            Moneda = payload.Moneda.Trim(),
            NumeroVersion = payload.NumeroVersion,
            FechaVigenciaDesde = payload.FechaVigenciaDesde,
            FechaVigenciaHasta = payload.FechaVigenciaHasta,
            Estado = payload.Estado.Trim(),
            UltimoEventoId = mensaje.EventId,
            OccurredAt = mensaje.OccurredAt
        };

        var filas = await _actualizador.ActualizarCostoMatriculaAsync(
            modelo,
            context.CancellationToken);

        RegistrarAplicacion(
            _logger,
            mensaje.EventId,
            payload.CostoMatriculaReferenciaId,
            filas);
    }

    [LoggerMessage(
        EventId = 7101,
        Level = LogLevel.Information,
        Message = "CostoMatriculaActualizado {EventId} aplicado sobre {CostoMatriculaId}: {Filas} fila(s).")]
    private static partial void RegistrarAplicacion(
        ILogger logger,
        Guid eventId,
        Guid costoMatriculaId,
        int filas);
}
