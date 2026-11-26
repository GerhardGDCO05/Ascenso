namespace Ascenso.Becas.Application.Renovacion.Puertos;

/// <summary>Consulta a MS-PRG la disponibilidad preliminar para una renovación.</summary>
public interface IPresupuestoRenovacionPrograma
{
    Task<bool?> TieneDisponibilidadAsync(
        Guid programaBecaId, Guid periodoSolicitadoId, decimal montoSolicitado,
        CancellationToken cancellationToken = default);
}
