using Ascenso.Becas.Infrastructure.Metricas;

namespace Ascenso.Becas.Api.Observabilidad;

/// <summary>
/// Actualiza periódicamente los gauges de compromisos pendientes y mensajes
/// del Outbox sin enviar. Un fallo de base de datos no detiene el servicio ni
/// expone detalles internos en el log.
/// </summary>
public sealed partial class PublicadorMetricasPendientes : BackgroundService
{
    private readonly IContadorPendientesOtorgamiento _contador;
    private readonly ILogger<PublicadorMetricasPendientes> _logger;
    private readonly TimeSpan _intervalo;

    public PublicadorMetricasPendientes(
        IContadorPendientesOtorgamiento contador,
        OpcionesTelemetria opciones,
        ILogger<PublicadorMetricasPendientes> logger)
    {
        _contador = contador ?? throw new ArgumentNullException(nameof(contador));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(opciones);

        _intervalo = opciones.IntervaloMetricasPendientes;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(_intervalo);

        try
        {
            while (await temporizador.WaitForNextTickAsync(stoppingToken))
            {
                await PublicarAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado ordenado del host.
        }
    }

    private async Task PublicarAsync(CancellationToken cancellationToken)
    {
        try
        {
            var pendientes = await _contador.ContarAsync(cancellationToken);

            MetricasOtorgamiento.ActualizarPendientes(
                pendientes.Compromisos,
                pendientes.Outbox);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            RegistrarFallo(_logger, excepcion.GetType().Name);
        }
    }

    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Warning,
        Message = "No se pudieron actualizar las métricas de pendientes: {tipo}.")]
    private static partial void RegistrarFallo(ILogger logger, string tipo);
}
