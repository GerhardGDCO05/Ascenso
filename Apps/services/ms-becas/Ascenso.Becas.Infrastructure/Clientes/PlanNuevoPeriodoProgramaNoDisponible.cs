using Ascenso.Becas.Application.Renovacion.Puertos;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>Falla cerrado hasta disponer del contrato oficial de MS-PRG.</summary>
public sealed class PlanNuevoPeriodoProgramaNoDisponible : IPlanNuevoPeriodoPrograma
{
    public Task<PlanNuevoPeriodo?> ObtenerAsync(Guid programaBecaId,
        Guid periodoSolicitadoId, CancellationToken cancellationToken = default)
        => Task.FromResult<PlanNuevoPeriodo?>(null);
}
