using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Infrastructure.Metricas;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Infrastructure.Dependencias;

/// <summary>
/// Registro de las métricas de negocio expuestas por el servicio.
/// </summary>
public static class RegistroMetricas
{
    public static IServiceCollection AgregarMetricas(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddSingleton<IMetricaConfiguracionFaltante, MetricaConfiguracionFaltante>();

        return servicios;
    }
}
