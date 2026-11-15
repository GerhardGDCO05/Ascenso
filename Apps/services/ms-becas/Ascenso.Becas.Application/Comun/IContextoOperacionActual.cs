namespace Ascenso.Becas.Application.Comun;

/// <summary>
/// Contexto de la operación actual. La implementación HTTP toma el usuario y
/// la correlación de la petición; los trabajos en segundo plano pueden no
/// tener usuario autenticado.
/// </summary>
public interface IContextoOperacionActual
{
    Guid? UsuarioId { get; }

    string CorrelationId { get; }

    string? DireccionIp { get; }

    Guid? AutorizacionOtpId { get; }
}
