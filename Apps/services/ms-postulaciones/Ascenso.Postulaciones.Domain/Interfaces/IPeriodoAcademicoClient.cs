using Ascenso.Postulaciones.Domain.Interfaces.Contratos;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto del calendario académico institucional para consultar el periodo
/// vigente.
/// </summary>
public interface IPeriodoAcademicoClient
{
    Task<PeriodoAcademicoActual> ObtenerPeriodoActualAsync(
        CancellationToken cancellationToken = default);
}
