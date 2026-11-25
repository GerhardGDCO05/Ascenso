namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>
/// Configuración de los trabajos recurrentes y de limpieza de Hangfire.
/// </summary>
public sealed class OpcionesTrabajosHangfire
{
    public const string Seccion = "Hangfire:Trabajos";

    /// <summary>
    /// Intervalo de la reconciliación de ofertas vencidas. El mínimo es un
    /// minuto y el máximo 59 para que la expresión cron sea válida.
    /// </summary>
    public int IntervaloReconciliacionMinutos { get; set; } = 5;

    /// <summary>
    /// Días de gracia que se conservan los registros de idempotencia después
    /// de su expiración. Cero elimina exactamente los vencidos.
    /// </summary>
    public int RetencionIdempotenciaDias { get; set; }

    /// <summary>
    /// Días que se conservan los mensajes ya enviados del Outbox.
    /// </summary>
    public int RetencionOutboxDias { get; set; } = 7;

    /// <summary>
    /// Máximo de ofertas vencidas que procesa cada reconciliación.
    /// </summary>
    public int LimiteOfertasPorReconciliacion { get; set; } = 500;

    /// <summary>
    /// Tamaño de cada página consultada al revisar suspensiones vigentes.
    /// </summary>
    public int LimiteSuspensionesPorPagina { get; set; } = 500;
}
