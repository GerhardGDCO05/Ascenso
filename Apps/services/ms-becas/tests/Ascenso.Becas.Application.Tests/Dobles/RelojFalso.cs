using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Reloj controlable por las pruebas.
/// </summary>
public sealed class RelojFalso : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
}
