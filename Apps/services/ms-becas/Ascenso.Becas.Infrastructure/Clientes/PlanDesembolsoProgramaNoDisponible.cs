using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>Falla cerrado hasta disponer del plan oficial del programa y periodo.</summary>
public sealed class PlanDesembolsoProgramaNoDisponible : IPlanDesembolsoPrograma
{
    public Task<PlanDesembolsoPrograma?> ObtenerAsync(Guid programaBecaId,
        Guid periodoAcademicoId, CancellationToken cancellationToken = default)
        => Task.FromResult<PlanDesembolsoPrograma?>(null);
}
