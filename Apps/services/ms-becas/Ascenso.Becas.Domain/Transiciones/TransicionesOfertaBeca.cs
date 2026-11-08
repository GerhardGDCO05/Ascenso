using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.Transiciones;

/// <summary>
/// Transiciones permitidas de <see cref="EstadoOferta"/>. Todos los estados
/// destino son terminales.
/// </summary>
public static class TransicionesOfertaBeca
{
    public static TablaTransiciones<EstadoOferta> Tabla { get; } = new(
        [
            (EstadoOferta.PendienteRespuesta, EstadoOferta.Aceptada),
            (EstadoOferta.PendienteRespuesta, EstadoOferta.RechazadaPorPostulante),
            (EstadoOferta.PendienteRespuesta, EstadoOferta.Expirada),
            (EstadoOferta.PendienteRespuesta, EstadoOferta.AnuladaPorCoordinacion)
        ],
        [
            EstadoOferta.Aceptada,
            EstadoOferta.RechazadaPorPostulante,
            EstadoOferta.Expirada,
            EstadoOferta.AnuladaPorCoordinacion
        ]);
}
