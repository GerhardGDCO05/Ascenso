namespace Ascenso.Becas.Domain.Enums;

/// <summary>
/// Ciclo de vida de una oferta de beca.
/// </summary>
public enum EstadoOferta
{
    PendienteRespuesta = 1,
    Aceptada = 2,
    RechazadaPorPostulante = 3,
    Expirada = 4,
    AnuladaPorCoordinacion = 5
}
