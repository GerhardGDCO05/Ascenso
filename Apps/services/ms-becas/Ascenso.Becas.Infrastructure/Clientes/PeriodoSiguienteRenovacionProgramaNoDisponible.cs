using Ascenso.Becas.Application.Renovacion.Puertos;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>Falla cerrado hasta disponer del calendario oficial entre periodos.</summary>
public sealed class PeriodoSiguienteRenovacionProgramaNoDisponible
    : IPeriodoSiguienteRenovacionPrograma
{
    public Task<PeriodoSiguienteRenovacionPrograma?> ObtenerAsync(
        Guid programaBecaId, Guid periodoOrigenId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<PeriodoSiguienteRenovacionPrograma?>(null);
}
