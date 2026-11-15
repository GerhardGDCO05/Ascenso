namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Vista de lectura del compromiso firmado del becario.
/// </summary>
public sealed record CompromisoBecarioDto(
    Guid CompromisoId,
    Guid BecaId,
    string Estado,
    int NumeroIntentoCarga,
    string? UrlPdfFirmado,
    DateTimeOffset? FechaCargaFirma,
    string? ObservacionesRevision,
    Guid? RevisadoPorCoordinadorId,
    DateTimeOffset? FechaRevision);
