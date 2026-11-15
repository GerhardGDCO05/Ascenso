namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Resultado de cargar un compromiso firmado.
/// </summary>
public sealed record CompromisoCargadoDto(
    Guid CompromisoId,
    int NumeroIntentoCarga,
    string Estado);
