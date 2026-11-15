using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace Ascenso.Becas.Api.Logging;

/// <summary>
/// Destruye objetos que contienen propiedades sensibles reemplazando sus
/// valores. Los objetos sin propiedades sensibles se dejan al formateador
/// predeterminado de Serilog.
/// </summary>
public sealed class RedaccionDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(propertyValueFactory);

        result = null!;

        if (EsEscalar(value.GetType()))
        {
            return false;
        }

        var propiedades = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(propiedad =>
                propiedad.CanRead
                && propiedad.GetIndexParameters().Length == 0)
            .ToList();

        if (!propiedades.Any(propiedad =>
                RedaccionDatosSensibles.EsClaveSensible(propiedad.Name)))
        {
            return false;
        }

        var valores = propiedades.Select(propiedad =>
            new LogEventProperty(
                propiedad.Name,
                RedaccionDatosSensibles.EsClaveSensible(propiedad.Name)
                    ? new ScalarValue(RedaccionDatosSensibles.Reemplazo)
                    : propertyValueFactory.CreatePropertyValue(
                        propiedad.GetValue(value),
                        destructureObjects: true)));

        result = new StructureValue(valores);
        return true;
    }

    private static bool EsEscalar(Type tipo)
        => tipo.IsPrimitive
            || tipo.IsEnum
            || tipo == typeof(string)
            || tipo == typeof(decimal)
            || tipo == typeof(DateTime)
            || tipo == typeof(DateTimeOffset)
            || tipo == typeof(TimeSpan)
            || tipo == typeof(Guid)
            || tipo == typeof(Uri)
            || tipo == typeof(DateOnly)
            || tipo == typeof(TimeOnly);
}
