using Ascenso.Becas.Application.Otorgamiento.Dtos;

namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Consultas de lectura del otorgamiento. Devuelve DTOs desacoplados del
/// modelo de persistencia.
/// </summary>
public interface IConsultasBeca
{
    Task<OfertaBecaDto?> ObtenerOfertaAsync(
        Guid ofertaId,
        CancellationToken cancellationToken = default);

    Task<BecaDto?> ObtenerBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BecaActivaDto>> ConsultarActivasAsync(
        Guid postulanteEstudianteId,
        Guid periodoAcademicoId,
        CancellationToken cancellationToken = default);
}
