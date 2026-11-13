using Ascenso.Programas.Application.Donantes.DTOs;

namespace Ascenso.Programas.Application.Donantes.Comandos.RegistrarDonante;

public sealed record class RegistrarDonanteCommand(
    string TipoDonante,
    string RazonSocialNombre,
    string IdentificacionFiscal,
    string Email,
    string Telefono,
    string DireccionFiscal,
    ContactoDonanteDto ContactoPrincipal,
    Guid UsuarioCreacionId);

