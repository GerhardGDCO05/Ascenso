namespace Ascenso.Programas.Application.Donantes.Comandos.RegistrarAporteDonante;

public sealed record class RegistrarAporteDonanteCommand(
    Guid DonanteId,
    decimal MontoAporte,
    string Moneda,
    string PeriodoAcademicoId,
    DateTimeOffset FechaAporte,
    string BancoEmisor,
    string NumeroReferenciaBancaria,
    string UrlComprobanteFirebase,
    string? Observaciones,
    Guid RegistradoPorUsuarioId);

