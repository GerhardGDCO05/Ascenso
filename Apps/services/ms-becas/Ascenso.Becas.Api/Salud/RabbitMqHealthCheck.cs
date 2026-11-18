using System.Globalization;
using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ascenso.Becas.Api.Salud;

/// <summary>
/// Verifica la conectividad con RabbitMQ cuando la mensajería está habilitada.
/// Si la mensajería está deshabilitada por configuración, el chequeo queda
/// sano porque RabbitMQ no es una dependencia obligatoria en ese despliegue.
/// </summary>
public sealed class RabbitMqHealthCheck : IHealthCheck
{
    public const string Nombre = "rabbitmq";

    private static readonly TimeSpan _tiempoMaximo = TimeSpan.FromSeconds(3);

    private readonly IConfiguration _configuracion;

    public RabbitMqHealthCheck(IConfiguration configuracion)
    {
        _configuracion = configuracion
            ?? throw new ArgumentNullException(nameof(configuracion));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(
                _configuracion["Mensajeria:Habilitada"],
                "false",
                StringComparison.OrdinalIgnoreCase))
        {
            return HealthCheckResult.Healthy(
                "Mensajería deshabilitada; RabbitMQ no es obligatorio.");
        }

        var host = _configuracion["RabbitMq:Host"] ?? "localhost";
        var puerto = LeerPuerto(_configuracion["RabbitMq:Port"]);

        using var tiempo = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        tiempo.CancelAfter(_tiempoMaximo);

        try
        {
            using var cliente = new TcpClient();
            await cliente.ConnectAsync(host, puerto, tiempo.Token);

            return HealthCheckResult.Healthy("RabbitMQ disponible.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                "La verificación de RabbitMQ superó el tiempo máximo.");
        }
        catch (SocketException)
        {
            return HealthCheckResult.Unhealthy(
                "No se pudo conectar a RabbitMQ.");
        }
    }

    private static int LeerPuerto(string? valor)
        => int.TryParse(
            valor,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var puerto)
            && puerto is > 0 and <= 65535
            ? puerto
            : 5672;
}
