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

    /// <summary>
    /// Obtiene los identificadores de programa y postulante de una oferta para
    /// que la capa HTTP evalúe las políticas de autorización basadas en
    /// recurso. Devuelve <c>null</c> cuando la oferta no existe.
    /// </summary>
    Task<RecursoOfertaDto?> ObtenerRecursoOfertaAsync(
        Guid ofertaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los identificadores de programa y postulante de una beca para
    /// que la capa HTTP evalúe las políticas de autorización basadas en
    /// recurso. Devuelve <c>null</c> cuando la beca no existe.
    /// </summary>
    Task<RecursoBecaDto?> ObtenerRecursoBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los identificadores de la beca, el programa y el postulante de
    /// un compromiso para que la capa HTTP evalúe las políticas de
    /// autorización basadas en recurso. Devuelve <c>null</c> cuando el
    /// compromiso no existe.
    /// </summary>
    Task<RecursoCompromisoDto?> ObtenerRecursoCompromisoAsync(
        Guid compromisoId,
        CancellationToken cancellationToken = default);

    Task<BecaDto?> ObtenerBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BecaActivaDto>> ConsultarActivasAsync(
        Guid postulanteEstudianteId,
        Guid periodoAcademicoId,
        CancellationToken cancellationToken = default);
}
