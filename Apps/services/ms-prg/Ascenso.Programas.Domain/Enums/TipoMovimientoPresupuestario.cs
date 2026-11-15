namespace Ascenso.Programas.Domain.Enums;

/// <summary>
/// Clasificación contable de un movimiento en la bitácora presupuestaria de un programa de becas.
/// </summary>
public enum TipoMovimientoPresupuestario
{
    AsignacionDonante = 1,
    ReajusteDonante = 2,
    ReservaCupo = 3,
    ConfirmacionDesembolso = 4,
    LiberacionReserva = 5,
    CierrePeriodo = 6
}
