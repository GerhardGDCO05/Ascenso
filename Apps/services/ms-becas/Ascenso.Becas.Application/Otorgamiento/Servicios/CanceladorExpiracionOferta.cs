using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.Application.Otorgamiento.Servicios;

internal static partial class CanceladorExpiracionOferta
{
    public static async Task CancelarAsync(
        IProgramadorExpiracionOfertas programador,
        ILogger logger,
        Guid ofertaId,
        string? referenciaJob)
    {
        ArgumentNullException.ThrowIfNull(programador);
        ArgumentNullException.ThrowIfNull(logger);

        if (string.IsNullOrWhiteSpace(referenciaJob))
        {
            RegistrarReferenciaFaltante(logger, ofertaId);
            return;
        }

        try
        {
            var cancelado = await programador.CancelarAsync(
                referenciaJob,
                CancellationToken.None);

            if (!cancelado)
            {
                RegistrarCancelacionNoConfirmada(logger, referenciaJob, ofertaId);
            }
        }
        catch (Exception excepcion)
        {
            RegistrarFalloCancelacion(logger, excepcion, referenciaJob, ofertaId);
        }
    }

    [LoggerMessage(
        EventId = 5100,
        Level = LogLevel.Warning,
        Message = "La oferta {OfertaId} no tiene referencia de expiración para cancelar.")]
    private static partial void RegistrarReferenciaFaltante(
        ILogger logger,
        Guid ofertaId);

    [LoggerMessage(
        EventId = 5101,
        Level = LogLevel.Warning,
        Message = "No se canceló el job {ReferenciaJob} de la oferta {OfertaId}; el job verificará el estado antes de expirar.")]
    private static partial void RegistrarCancelacionNoConfirmada(
        ILogger logger,
        string referenciaJob,
        Guid ofertaId);

    [LoggerMessage(
        EventId = 5102,
        Level = LogLevel.Warning,
        Message = "Falló la cancelación del job {ReferenciaJob} de la oferta {OfertaId}; el job verificará el estado antes de expirar.")]
    private static partial void RegistrarFalloCancelacion(
        ILogger logger,
        Exception exception,
        string referenciaJob,
        Guid ofertaId);
}
