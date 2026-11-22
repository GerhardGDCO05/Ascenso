using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Doble en memoria de las métricas de negocio del otorgamiento.
/// </summary>
public sealed class MetricasOtorgamientoFalsas : IMetricasOtorgamiento
{
    public int Emitidas { get; private set; }

    public int Aceptadas { get; private set; }

    public int Rechazadas { get; private set; }

    public int Expiradas { get; private set; }

    public void OfertaEmitida() => Emitidas++;

    public void OfertaAceptada() => Aceptadas++;

    public void OfertaRechazada() => Rechazadas++;

    public void OfertaExpirada() => Expiradas++;
}
