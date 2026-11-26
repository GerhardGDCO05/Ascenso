using Ascenso.Becas.Application.Renovacion.Puertos;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>Falla cerrado hasta que MS-PRG ofrezca liberar cupos ocupados.</summary>
public sealed class LiberacionCupoProgramaNoDisponible : ILiberacionCupoPrograma
{
    public Task<bool?> LiberarPorConclusionAsync(
        Guid programaBecaId, Guid becaId, Guid periodoVigenteId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<bool?>(null);
}
