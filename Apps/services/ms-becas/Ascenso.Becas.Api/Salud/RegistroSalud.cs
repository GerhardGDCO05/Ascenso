using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Ascenso.Becas.Api.Salud;

/// <summary>
/// Registra y expone los endpoints de salud separados para liveness y
/// readiness.
/// </summary>
public static class RegistroSalud
{
    public const string EtiquetaReadiness = "ready";

    private static readonly TimeSpan _tiempoMaximoCheck =
        TimeSpan.FromSeconds(5);

    public static IServiceCollection AgregarChequeosDeSalud(
        this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>(
                PostgresHealthCheck.Nombre,
                tags: [EtiquetaReadiness],
                timeout: _tiempoMaximoCheck)
            .AddCheck<RabbitMqHealthCheck>(
                RabbitMqHealthCheck.Nombre,
                tags: [EtiquetaReadiness],
                timeout: _tiempoMaximoCheck);

        return servicios;
    }

    public static WebApplication MapearSalud(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = EscritorRespuestaSalud.EscribirAsync,
            AllowCachingResponses = false
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registro =>
                registro.Tags.Contains(EtiquetaReadiness),
            ResponseWriter = EscritorRespuestaSalud.EscribirAsync,
            AllowCachingResponses = false
        }).AllowAnonymous();

        return app;
    }
}
