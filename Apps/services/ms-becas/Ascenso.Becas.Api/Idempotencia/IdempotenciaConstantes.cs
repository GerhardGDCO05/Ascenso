namespace Ascenso.Becas.Api.Idempotencia;

/// <summary>
/// Constantes del protocolo de idempotencia HTTP.
/// </summary>
public static class IdempotenciaConstantes
{
    public const string Encabezado = "Idempotency-Key";

    public const string EncabezadoReplay = "Idempotent-Replayed";

    public const string EstadoEnProceso = "EN_PROCESO";

    public const string EstadoCompletada = "COMPLETADA";

    public const int LongitudMinimaClave = 8;

    public const int LongitudMaximaClave = 128;

    public const string RutaBase = "/api/v1";

    public static readonly TimeSpan Vigencia = TimeSpan.FromDays(7);
}
