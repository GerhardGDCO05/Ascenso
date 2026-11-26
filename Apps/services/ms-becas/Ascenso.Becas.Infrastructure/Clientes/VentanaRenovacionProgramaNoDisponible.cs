using Ascenso.Becas.Application.Renovacion.Puertos;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>Falla cerrado hasta que MS-PRG publique la ventana por programa y periodo.</summary>
public sealed class VentanaRenovacionProgramaNoDisponible : IVentanaRenovacionPrograma
{
    public Task<VentanaRenovacionPrograma?> ObtenerAsync(
        Guid programaBecaId, Guid periodoSolicitadoId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<VentanaRenovacionPrograma?>(null);
}
