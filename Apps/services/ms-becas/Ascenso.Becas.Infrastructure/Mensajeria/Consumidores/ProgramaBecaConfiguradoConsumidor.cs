using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.ValueObjects;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Ascenso.Becas.Infrastructure.Persistence.Proyecciones;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.Infrastructure.Mensajeria.Consumidores;

/// <summary>
/// Aplica <c>ProgramaBecaConfigurado</c> a la proyección local con upsert
/// idempotente. Un evento repetido o anterior no retrocede la configuración.
/// </summary>
public sealed partial class ProgramaBecaConfiguradoConsumidor
    : IConsumer<EventoIntegracion<ProgramaBecaConfigurado>>
{
    private readonly ActualizadorProyeccionesRef _actualizador;
    private readonly ILogger<ProgramaBecaConfiguradoConsumidor> _logger;

    public ProgramaBecaConfiguradoConsumidor(
        ActualizadorProyeccionesRef actualizador,
        ILogger<ProgramaBecaConfiguradoConsumidor> logger)
    {
        _actualizador = actualizador ?? throw new ArgumentNullException(nameof(actualizador));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(
        ConsumeContext<EventoIntegracion<ProgramaBecaConfigurado>> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var mensaje = context.Message;
        var payload = mensaje.Payload
            ?? throw new InvalidOperationException("El evento no contiene el payload de programa.");

        CodigosIntegracion.ValidarEvento(
            mensaje.EventId,
            mensaje.AggregateId,
            nameof(ProgramaBecaConfigurado));

        if (payload.ProgramaBecaId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.Codigo)
            || string.IsNullOrWhiteSpace(payload.Nombre)
            || string.IsNullOrWhiteSpace(payload.Estado)
            || payload.DuracionPeriodos <= 0
            || payload.PoliticaCobertura is null
            || payload.CondicionesPermanencia is null)
        {
            throw new InvalidOperationException(
                "El evento ProgramaBecaConfigurado está incompleto o es inválido.");
        }

        var modelo = new ProgramaBecaRef
        {
            ProgramaBecaId = payload.ProgramaBecaId,
            Codigo = payload.Codigo.Trim(),
            Nombre = payload.Nombre.Trim(),
            PoliticaCobertura = new PoliticaCobertura(
                CodigosIntegracion.ADominio<TipoCobertura>(
                    payload.PoliticaCobertura.Modalidad,
                    "politicaCobertura.modalidad"),
                payload.PoliticaCobertura.PorcentajeCobertura,
                payload.PoliticaCobertura.MontoFijoPeriodo,
                payload.PoliticaCobertura.Moneda),
            CondicionesPermanencia = new CondicionesPermanencia(
                payload.CondicionesPermanencia.PromedioMinimoRenovacion,
                payload.CondicionesPermanencia.MateriasMinimasAprobadas,
                payload.CondicionesPermanencia.MaximoMateriasReprobadas,
                payload.CondicionesPermanencia.HorasServicioPorPeriodo,
                CodigosIntegracion.ADominio<ConsecuenciaPermanencia>(
                    payload.CondicionesPermanencia.ConsecuenciaIncumplimiento,
                    "condicionesPermanencia.consecuenciaIncumplimiento")),
            PermiteAcumulacion = payload.PermiteAcumulacion,
            RequiereCompromisoFirmado = payload.RequiereCompromisoFirmado,
            DuracionPeriodos = payload.DuracionPeriodos,
            Estado = payload.Estado.Trim(),
            UltimoEventoId = mensaje.EventId,
            OccurredAt = mensaje.OccurredAt
        };

        var filas = await _actualizador.ActualizarProgramaAsync(
            modelo,
            context.CancellationToken);

        RegistrarAplicacion(
            _logger,
            mensaje.EventId,
            payload.ProgramaBecaId,
            filas);
    }

    [LoggerMessage(
        EventId = 7100,
        Level = LogLevel.Information,
        Message = "ProgramaBecaConfigurado {EventId} aplicado sobre {ProgramaBecaId}: {Filas} fila(s).")]
    private static partial void RegistrarAplicacion(
        ILogger logger,
        Guid eventId,
        Guid programaBecaId,
        int filas);
}
