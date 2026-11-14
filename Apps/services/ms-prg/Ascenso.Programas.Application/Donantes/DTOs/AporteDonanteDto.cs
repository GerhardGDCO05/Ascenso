namespace Ascenso.Programas.Application.Donantes.DTOs;

public sealed record class AporteDonanteDto(
    Guid Id,
    Guid DonanteId,
    decimal MontoAporte,
    string Moneda,
    string PeriodoAcademicoId,
    DateTimeOffset FechaAporte,
    string BancoEmisor,
    string NumeroReferenciaBancaria,
    string UrlComprobanteFirebase,
    string EstadoAporte,
    string? Observaciones,
    DateTimeOffset FechaCreacion,
    Guid RegistradoPorUsuarioId);

