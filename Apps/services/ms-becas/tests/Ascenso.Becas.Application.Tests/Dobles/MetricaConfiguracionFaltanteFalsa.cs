using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Doble en memoria de la métrica de configuración faltante.
/// </summary>
public sealed class MetricaConfiguracionFaltanteFalsa : IMetricaConfiguracionFaltante
{
    public List<string> Recursos { get; } = [];

    public void Registrar(string recurso) => Recursos.Add(recurso);
}
