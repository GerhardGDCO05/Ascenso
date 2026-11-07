using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.Transiciones;

/// <summary>
/// Transiciones permitidas de <see cref="EstadoCompromiso"/>. Un compromiso
/// rechazado puede volver a revisión cuando el becario carga un nuevo intento.
/// </summary>
public static class TransicionesCompromisoBecario
{
    public static TablaTransiciones<EstadoCompromiso> Tabla { get; } = new(
        [
            (EstadoCompromiso.PendienteCarga, EstadoCompromiso.EnRevision),
            (EstadoCompromiso.EnRevision, EstadoCompromiso.Aprobado),
            (EstadoCompromiso.EnRevision, EstadoCompromiso.Rechazado),
            (EstadoCompromiso.Rechazado, EstadoCompromiso.EnRevision)
        ],
        [
            EstadoCompromiso.Aprobado
        ]);
}
