using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.Transiciones;

/// <summary>Duplas permitidas por la Tabla 3.3 del DDD.</summary>
public static class TransicionesDesembolso
{
    public static TablaTransiciones<EstadoDesembolso> Tabla { get; } = new(
        [
            (EstadoDesembolso.Programado, EstadoDesembolso.EnProceso),
            (EstadoDesembolso.Programado, EstadoDesembolso.Suspendido),
            (EstadoDesembolso.Suspendido, EstadoDesembolso.Programado),
            (EstadoDesembolso.EnProceso, EstadoDesembolso.PagadoConfirmado),
            (EstadoDesembolso.EnProceso, EstadoDesembolso.Fallido),
            (EstadoDesembolso.EnProceso, EstadoDesembolso.Devuelto),
            (EstadoDesembolso.Fallido, EstadoDesembolso.Programado),
            (EstadoDesembolso.Devuelto, EstadoDesembolso.Programado),
            (EstadoDesembolso.Programado, EstadoDesembolso.Cancelado),
            (EstadoDesembolso.Suspendido, EstadoDesembolso.Cancelado)
        ],
        [EstadoDesembolso.PagadoConfirmado, EstadoDesembolso.Cancelado]);
}
