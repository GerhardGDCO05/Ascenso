using Ascenso.Becas.Application.Comun;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Contexto de operación controlable por las pruebas.
/// </summary>
public sealed class ContextoOperacionFalso : IContextoOperacionActual
{
    public Guid? UsuarioId { get; set; } = Guid.CreateVersion7();

    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

    public string? DireccionIp { get; set; } = "192.168.1.10";

    public Guid? AutorizacionOtpId { get; set; }
}
