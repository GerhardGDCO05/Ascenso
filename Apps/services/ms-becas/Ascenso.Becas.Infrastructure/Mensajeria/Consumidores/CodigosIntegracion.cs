namespace Ascenso.Becas.Infrastructure.Mensajeria.Consumidores;

/// <summary>
/// Convierte los códigos MAYÚSCULAS de los contratos de integración a las
/// enumeraciones del dominio.
/// </summary>
internal static class CodigosIntegracion
{
    public static TEnum ADominio<TEnum>(string? codigo, string campo)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new InvalidOperationException($"El campo '{campo}' es obligatorio.");
        }

        var normalizado = codigo.Trim().Replace("_", string.Empty, StringComparison.Ordinal);

        if (Enum.TryParse<TEnum>(normalizado, ignoreCase: true, out var valor)
            && Enum.IsDefined(valor))
        {
            return valor;
        }

        throw new InvalidOperationException(
            $"El valor '{codigo}' del campo '{campo}' no es válido.");
    }

    public static void ValidarEvento(Guid eventId, Guid aggregateId, string nombreEvento)
    {
        if (eventId == Guid.Empty)
        {
            throw new InvalidOperationException($"El evento '{nombreEvento}' no tiene eventId.");
        }

        if (aggregateId == Guid.Empty)
        {
            throw new InvalidOperationException($"El evento '{nombreEvento}' no tiene aggregateId.");
        }
    }
}
