namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Resultado de la revisión de un compromiso firmado.
/// </summary>
public sealed record CompromisoRevisadoDto(
    Guid CompromisoId,
    string Estado,
    bool HabilitadaParaDesembolso);
