namespace Ascenso.Becas.Domain.Ports;

public interface IPeriodosAcademicosClient
{
    Task<ResultadoPeriodoAcademico> ObtenerActualAsync(
        CancellationToken cancellationToken = default);
}
