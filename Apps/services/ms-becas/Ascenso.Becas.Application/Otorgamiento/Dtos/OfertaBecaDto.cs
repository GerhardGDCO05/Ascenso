namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Vista de lectura de una oferta de beca. No contiene datos personales del
/// postulante ni información bancaria.
/// </summary>
public sealed record OfertaBecaDto(
    Guid OfertaId,
    Guid BecaId,
    Guid PostulacionId,
    Guid ProgramaBecaId,
    string? NombrePrograma,
    Guid PeriodoAcademicoId,
    string Estado,
    decimal MontoOfertaPorPeriodo,
    decimal MontoTotalEstimadoDuracion,
    int DuracionPeriodos,
    CondicionesPermanenciaDto CondicionesSnapshot,
    string UrlCartaPdf,
    DateTimeOffset FechaEmision,
    DateTimeOffset FechaVencimientoAceptacion,
    DateTimeOffset? FechaRespuesta,
    string? MotivoRechazoOAnulacion);
