using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Infrastructure.Tiempo;

/// <summary>
/// Implementación del reloj basada en la hora UTC del sistema.
/// </summary>
public sealed class RelojSistema : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
