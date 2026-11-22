using System.Diagnostics;

namespace Ascenso.Becas.Infrastructure.Observabilidad;

/// <summary>
/// Instrumentación de los jobs de Hangfire. Hangfire no publica actividades
/// propias, por lo que cada job abre una actividad en la fuente
/// <see cref="NombreFuente"/> y la exporta OpenTelemetry.
/// </summary>
public static class TelemetriaHangfire
{
    public const string NombreFuente = "Ascenso.Becas.Hangfire";

    public const string PropiedadTrabajo = "hangfire.job";

    private static readonly ActivitySource _fuente = new(NombreFuente, "1.0.0");

    public static async Task EjecutarAsync(
        string trabajo,
        Func<Task> ejecutar)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trabajo);
        ArgumentNullException.ThrowIfNull(ejecutar);

        using var actividad = _fuente.StartActivity(
            $"hangfire {trabajo}",
            ActivityKind.Internal);

        actividad?.SetTag(PropiedadTrabajo, trabajo);

        try
        {
            await ejecutar();
        }
        catch (Exception excepcion)
        {
            actividad?.SetStatus(
                ActivityStatusCode.Error,
                excepcion.GetType().Name);
            throw;
        }
    }

    public static async Task<T> EjecutarAsync<T>(
        string trabajo,
        Func<Task<T>> ejecutar)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trabajo);
        ArgumentNullException.ThrowIfNull(ejecutar);

        using var actividad = _fuente.StartActivity(
            $"hangfire {trabajo}",
            ActivityKind.Internal);

        actividad?.SetTag(PropiedadTrabajo, trabajo);

        try
        {
            return await ejecutar();
        }
        catch (Exception excepcion)
        {
            actividad?.SetStatus(
                ActivityStatusCode.Error,
                excepcion.GetType().Name);
            throw;
        }
    }
}
