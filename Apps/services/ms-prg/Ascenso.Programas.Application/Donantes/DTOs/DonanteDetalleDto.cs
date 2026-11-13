namespace Ascenso.Programas.Application.Donantes.DTOs;

public sealed record class DonanteDetalleDto(
    Guid Id,
    string TipoDonante,
    string RazonSocialNombre,
    string IdentificacionFiscal,
    string Email,
    string Telefono,
    string DireccionFiscal,
    ContactoDonanteDto ContactoPrincipal,
    string EstadoDonante,
    string Moneda,
    decimal SaldoTotalHistorico,
    decimal SaldoDisponibleAsignacion,
    DateTimeOffset FechaCreacion,
    Guid UsuarioCreacionId,
    DateTimeOffset FechaUltimaModificacion,
    Guid UsuarioModificacionId,
    int Version);

