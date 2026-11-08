namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de consulta al microservicio de postulaciones.
/// </summary>
public interface IPostulacionesClient
{
    Task<ResultadoAdjudicabilidad> ObtenerAdjudicabilidadAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);
}
