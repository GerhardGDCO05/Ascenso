namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Fuente única de tiempo para el dominio y los casos de uso.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
