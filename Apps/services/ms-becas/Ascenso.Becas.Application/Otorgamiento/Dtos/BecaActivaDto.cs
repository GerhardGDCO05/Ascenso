namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Vista mínima para la consulta interna de becas activas. Solo expone
/// identificadores, estado y datos de cobertura; nunca datos personales ni
/// bancarios.
/// </summary>
public sealed record BecaActivaDto(
    Guid BecaId,
    Guid ProgramaBecaId,
    Guid PeriodoAcademicoId,
    string Estado,
    string ModalidadCobertura,
    decimal PorcentajeCobertura,
    decimal MontoFijoPeriodo,
    string Moneda,
    bool PermiteAcumulacion);
