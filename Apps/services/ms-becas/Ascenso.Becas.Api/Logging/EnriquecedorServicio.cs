using Serilog.Core;
using Serilog.Events;

namespace Ascenso.Becas.Api.Logging;

/// <summary>
/// Identifica el servicio y el entorno en todos los eventos de log para que
/// los agregadores puedan separar las señales de cada despliegue.
/// </summary>
public sealed class EnriquecedorServicio : ILogEventEnricher
{
    public const string PropiedadServicio = "Service";

    public const string PropiedadEntorno = "Environment";

    public const string PropiedadVersion = "ServiceVersion";

    private readonly string _servicio;
    private readonly string _entorno;
    private readonly string _version;

    public EnriquecedorServicio(
        string servicio,
        string entorno,
        string? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(servicio);
        ArgumentException.ThrowIfNullOrWhiteSpace(entorno);

        _servicio = servicio;
        _entorno = entorno;
        _version = string.IsNullOrWhiteSpace(version)
            ? typeof(EnriquecedorServicio).Assembly
                .GetName()
                .Version?
                .ToString() ?? "desconocida"
            : version;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(PropiedadServicio, _servicio));
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(PropiedadEntorno, _entorno));
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(PropiedadVersion, _version));
    }
}
