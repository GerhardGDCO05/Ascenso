using Serilog.Core;
using Serilog.Events;

namespace Ascenso.Becas.Api.Logging;

/// <summary>
/// Reemplaza por <see cref="RedaccionDatosSensibles.Reemplazo"/> las
/// propiedades escalares cuyo nombre corresponde a un dato sensible.
/// </summary>
public sealed class RedaccionLogEventEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        foreach (var (nombre, _) in logEvent.Properties.ToList())
        {
            if (!RedaccionDatosSensibles.EsClaveSensible(nombre))
            {
                continue;
            }

            logEvent.AddOrUpdateProperty(new LogEventProperty(
                nombre,
                new ScalarValue(RedaccionDatosSensibles.Reemplazo)));
        }
    }
}
