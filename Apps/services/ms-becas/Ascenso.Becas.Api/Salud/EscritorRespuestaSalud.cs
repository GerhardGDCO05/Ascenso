using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ascenso.Becas.Api.Salud;

/// <summary>
/// Escribe el informe de salud como JSON sin exponer excepciones, cadenas de
/// conexión, credenciales ni otros detalles internos.
/// </summary>
public static class EscritorRespuestaSalud
{
    public static async Task EscribirAsync(
        HttpContext contexto,
        HealthReport informe)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(informe);

        var respuesta = new RespuestaSalud(
            informe.Status.ToString(),
            Math.Round(informe.TotalDuration.TotalMilliseconds, 2),
            informe.Entries
                .OrderBy(entrada => entrada.Key, StringComparer.Ordinal)
                .Select(entrada => new ChequeoSalud(
                    entrada.Key,
                    entrada.Value.Status.ToString(),
                    entrada.Value.Description,
                    Math.Round(entrada.Value.Duration.TotalMilliseconds, 2)))
                .ToArray());

        await contexto.Response.WriteAsJsonAsync(
            respuesta,
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
