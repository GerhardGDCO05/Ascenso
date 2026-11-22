using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ascenso.Becas.Api.Salud;

/// <summary>
/// Escribe el informe de salud como JSON sin exponer excepciones, cadenas de
/// conexión, credenciales ni otros detalles internos. En producción omite las
/// descripciones de los chequeos para no filtrar información operativa.
/// </summary>
public static class EscritorRespuestaSalud
{
    private static readonly JsonSerializerOptions _opciones = new(
        JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task EscribirAsync(
        HttpContext contexto,
        HealthReport informe)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(informe);

        var produccion = contexto.RequestServices
            .GetService<IHostEnvironment>()?
            .IsProduction() == true;

        var respuesta = new RespuestaSalud(
            informe.Status.ToString(),
            Math.Round(informe.TotalDuration.TotalMilliseconds, 2),
            informe.Entries
                .OrderBy(entrada => entrada.Key, StringComparer.Ordinal)
                .Select(entrada => new ChequeoSalud(
                    entrada.Key,
                    entrada.Value.Status.ToString(),
                    produccion ? null : entrada.Value.Description,
                    Math.Round(entrada.Value.Duration.TotalMilliseconds, 2)))
                .ToArray());

        await contexto.Response.WriteAsJsonAsync(
            respuesta,
            _opciones,
            contexto.RequestAborted);
    }

    private sealed record RespuestaSalud(
        string Status,
        double TotalDurationMs,
        IReadOnlyList<ChequeoSalud> Checks);

    private sealed record ChequeoSalud(
        string Name,
        string Status,
        string? Description,
        double DurationMs);
}
