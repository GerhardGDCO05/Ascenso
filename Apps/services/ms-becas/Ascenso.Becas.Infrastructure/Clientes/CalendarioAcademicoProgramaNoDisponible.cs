using Ascenso.Becas.Application.SeguimientoAcademico.Puertos;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// MS-PRG aún no publica el contrato de calendario académico. La ausencia de
/// configuración bloquea el registro en vez de inventar una fecha límite.
/// </summary>
public sealed class CalendarioAcademicoProgramaNoDisponible : ICalendarioAcademicoPrograma
{
    public Task<DateOnly?> ObtenerFechaLimiteReporteAsync(
        Guid programaBecaId, Guid periodoAcademicoId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<DateOnly?>(null);
}
