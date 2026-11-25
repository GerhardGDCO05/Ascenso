using Ascenso.Programas.Infrastructure.Trabajos;
using Hangfire;
using Hangfire.PostgreSql;

namespace Ascenso.Programas.Api.Configuracion;

/// <summary>
/// Registro de Hangfire sobre PostgreSQL y del job recurrente de cierre automático.
/// </summary>
public static class ConfiguracionHangfire
{
    public const string IdJobCierre = "cerrar-convocatorias-vencidas";

    public static IServiceCollection AgregarHangfireProgramas(
        this IServiceCollection services,
        string cadenaConexion,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var habilitado = configuration.GetValue("Hangfire:Habilitado", true);
        if (!habilitado || string.IsNullOrWhiteSpace(cadenaConexion))
        {
            return services;
        }

        services.AddHangfire(configuracion => configuracion
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(opciones => opciones.UseNpgsqlConnection(cadenaConexion)));

        services.AddHangfireServer(opciones => opciones.WorkerCount = 1);

        return services;
    }

    public static WebApplication UsarHangfireProgramas(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Services.GetService<IGlobalConfiguration>() is null)
        {
            return app;
        }

        var intervalo = app.Configuration["Hangfire:IntervaloCierre"] ?? Cron.Minutely();

        app.Services
            .GetRequiredService<IRecurringJobManager>()
            .AddOrUpdate<CerrarConvocatoriasVencidasJob>(
                IdJobCierre,
                trabajo => trabajo.EjecutarAsync(CancellationToken.None),
                intervalo,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        return app;
    }
}
