using System.Data.Common;
using Ascenso.Becas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ascenso.Becas.Api.Salud;

/// <summary>
/// Verifica que PostgreSQL esté disponible y que no existan migraciones
/// pendientes de aplicar.
/// </summary>
public sealed class PostgresHealthCheck : IHealthCheck
{
    public const string Nombre = "postgresql";

    private static readonly TimeSpan _tiempoMaximo = TimeSpan.FromSeconds(3);

    private readonly IServiceScopeFactory _alcances;

    public PostgresHealthCheck(IServiceScopeFactory alcances)
    {
        _alcances = alcances ?? throw new ArgumentNullException(nameof(alcances));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var tiempo = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        tiempo.CancelAfter(_tiempoMaximo);

        try
        {
            await using var alcance = _alcances.CreateAsyncScope();
            var baseDatos = alcance.ServiceProvider
                .GetRequiredService<BecasDbContext>()
                .Database;

            if (!await baseDatos.CanConnectAsync(tiempo.Token))
            {
                return HealthCheckResult.Unhealthy(
                    "No se pudo conectar a PostgreSQL.");
            }

            var pendientes = (await baseDatos.GetPendingMigrationsAsync(
                    tiempo.Token))
                .ToArray();

            return pendientes.Length == 0
                ? HealthCheckResult.Healthy(
                    "PostgreSQL disponible y migraciones aplicadas.")
                : HealthCheckResult.Unhealthy(
                    $"PostgreSQL disponible con {pendientes.Length} "
                    + "migraciones pendientes.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                "La verificación de PostgreSQL superó el tiempo máximo.");
        }
        catch (DbException)
        {
            return HealthCheckResult.Unhealthy(
                "No se pudo conectar a PostgreSQL.");
        }
        catch (InvalidOperationException)
        {
            return HealthCheckResult.Unhealthy(
                "No se pudo verificar el estado de PostgreSQL.");
        }
    }
}
