using Ascenso.Becas.Application.Renovacion.Puertos;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>Falla cerrado hasta acordar la consulta presupuestaria con MS-PRG.</summary>
public sealed class PresupuestoRenovacionProgramaNoDisponible
    : IPresupuestoRenovacionPrograma
{
    public Task<bool?> TieneDisponibilidadAsync(
        Guid programaBecaId, Guid periodoSolicitadoId, decimal montoSolicitado,
        CancellationToken cancellationToken = default)
        => Task.FromResult<bool?>(null);
}
