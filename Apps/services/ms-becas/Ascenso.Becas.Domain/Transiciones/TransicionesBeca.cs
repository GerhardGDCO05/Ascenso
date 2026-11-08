using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.Transiciones;

/// <summary>
/// Transiciones permitidas de <see cref="EstadoBeca"/> según la Tabla 3.2
/// de la especificación maestra.
/// </summary>
public static class TransicionesBeca
{
    public static TablaTransiciones<EstadoBeca> Tabla { get; } = new(
        [
            (EstadoBeca.Otorgada, EstadoBeca.Activa),
            (EstadoBeca.Otorgada, EstadoBeca.Revocada),
            (EstadoBeca.Activa, EstadoBeca.Suspendida),
            (EstadoBeca.Activa, EstadoBeca.Renovada),
            (EstadoBeca.Activa, EstadoBeca.Concluida),
            (EstadoBeca.Activa, EstadoBeca.Revocada),
            (EstadoBeca.Suspendida, EstadoBeca.Activa),
            (EstadoBeca.Suspendida, EstadoBeca.Revocada)
        ],
        [
            EstadoBeca.Renovada,
            EstadoBeca.Concluida,
            EstadoBeca.Revocada
        ]);
}
