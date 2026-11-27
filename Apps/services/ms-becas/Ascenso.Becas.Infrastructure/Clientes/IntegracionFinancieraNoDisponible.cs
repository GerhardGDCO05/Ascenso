using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Infrastructure.Clientes;

public sealed class MovimientoPresupuestoProgramaNoDisponible : IMovimientoPresupuestoPrograma
{
    public Task AsegurarDisponibleAsync(CancellationToken cancellationToken = default)
        => throw new DependenciaNoDisponibleException("MS-PRG: movimientos-presupuesto");

    public Task RegistrarAsync(MovimientoPresupuestoPrograma movimiento,
        CancellationToken cancellationToken = default)
        => throw new DependenciaNoDisponibleException("MS-PRG: movimientos-presupuesto");
}

public sealed class CuentaInstitucionalProgramaNoDisponible : ICuentaInstitucionalPrograma
{
    public Task<string?> ObtenerSnapshotCifradoAsync(Guid programaBecaId,
        Guid periodoAcademicoId, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}
