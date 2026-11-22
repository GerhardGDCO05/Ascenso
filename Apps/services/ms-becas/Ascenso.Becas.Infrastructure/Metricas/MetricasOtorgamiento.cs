using System.Diagnostics.Metrics;
using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Infrastructure.Metricas;

/// <summary>
/// Métricas de negocio del otorgamiento expuestas con
/// <see cref="System.Diagnostics.Metrics"/>. Los contadores se incrementan
/// desde los casos de uso y los gauges de pendientes los actualiza el
/// publicador periódico.
/// </summary>
public sealed class MetricasOtorgamiento : IMetricasOtorgamiento
{
    public const string NombreMedidor = "Ascenso.Becas";

    public const string NombreOfertasEmitidas = "ofertas_emitidas";

    public const string NombreOfertasAceptadas = "ofertas_aceptadas";

    public const string NombreOfertasRechazadas = "ofertas_rechazadas";

    public const string NombreOfertasExpiradas = "ofertas_expiradas";

    public const string NombreCompromisosPendientes = "compromisos_pendientes";

    public const string NombreOutboxPendientes = "outbox_pendientes";

    private static readonly Meter _medidor = new(NombreMedidor, "1.0.0");

    private static readonly Counter<long> _ofertasEmitidas = _medidor.CreateCounter<long>(
        NombreOfertasEmitidas,
        unit: "{oferta}",
        description: "Ofertas de beca emitidas correctamente.");

    private static readonly Counter<long> _ofertasAceptadas = _medidor.CreateCounter<long>(
        NombreOfertasAceptadas,
        unit: "{oferta}",
        description: "Ofertas de beca aceptadas por el postulante.");

    private static readonly Counter<long> _ofertasRechazadas = _medidor.CreateCounter<long>(
        NombreOfertasRechazadas,
        unit: "{oferta}",
        description: "Ofertas de beca rechazadas por el postulante.");

    private static readonly Counter<long> _ofertasExpiradas = _medidor.CreateCounter<long>(
        NombreOfertasExpiradas,
        unit: "{oferta}",
        description: "Ofertas de beca expiradas por vencimiento.");

    private static long _compromisosPendientes;

    private static long _outboxPendientes;

    static MetricasOtorgamiento()
    {
        _medidor.CreateObservableGauge(
            NombreCompromisosPendientes,
            () => Interlocked.Read(ref _compromisosPendientes),
            unit: "{compromiso}",
            description: "Compromisos pendientes de carga o en revisión.");

        _medidor.CreateObservableGauge(
            NombreOutboxPendientes,
            () => Interlocked.Read(ref _outboxPendientes),
            unit: "{mensaje}",
            description: "Mensajes del Outbox pendientes de envío.");
    }

    public void OfertaEmitida() => _ofertasEmitidas.Add(1);

    public void OfertaAceptada() => _ofertasAceptadas.Add(1);

    public void OfertaRechazada() => _ofertasRechazadas.Add(1);

    public void OfertaExpirada() => _ofertasExpiradas.Add(1);

    /// <summary>
    /// Publica los valores actuales de los gauges de pendientes. Lo invoca el
    /// publicador periódico después de consultar la base de datos.
    /// </summary>
    public static void ActualizarPendientes(long compromisos, long outbox)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(compromisos);
        ArgumentOutOfRangeException.ThrowIfNegative(outbox);

        Interlocked.Exchange(ref _compromisosPendientes, compromisos);
        Interlocked.Exchange(ref _outboxPendientes, outbox);
    }
}
