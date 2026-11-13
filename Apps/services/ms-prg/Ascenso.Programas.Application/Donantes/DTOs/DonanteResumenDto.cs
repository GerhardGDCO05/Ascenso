namespace Ascenso.Programas.Application.Donantes.DTOs;

public sealed record class DonanteResumenDto(
    Guid Id,
    string TipoDonante,
    string RazonSocialNombre,
    string IdentificacionFiscal,
    string Email,
    string EstadoDonante,
    string Moneda,
    decimal SaldoTotalHistorico,
    decimal SaldoDisponibleAsignacion,
    DateTimeOffset FechaCreacion,
    int Version);

