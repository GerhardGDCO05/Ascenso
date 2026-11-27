namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

public enum TipoMovimientoPresupuestoPrograma
{
    DesembolsoConfirmado = 1,
    Suspension = 2,
    Reanudacion = 3,
    Revocacion = 4
}

public sealed record MovimientoPresupuestoPrograma(
    Guid ProgramaBecaId, Guid BecaId, Guid PeriodoAcademicoId,
    Guid ReferenciaId, TipoMovimientoPresupuestoPrograma Tipo,
    decimal Monto, string ClaveIdempotencia);

/// <summary>Movimiento contable síncrono que debe confirmarse antes del commit local.</summary>
public interface IMovimientoPresupuestoPrograma
{
    Task AsegurarDisponibleAsync(CancellationToken cancellationToken = default);

    Task RegistrarAsync(MovimientoPresupuestoPrograma movimiento,
        CancellationToken cancellationToken = default);
}
