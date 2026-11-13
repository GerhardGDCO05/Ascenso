using Ascenso.Programas.Application.Donantes.DTOs;

namespace Ascenso.Programas.Application.Donantes.Comandos.ActualizarContactoDonante;

public sealed record class ActualizarContactoDonanteCommand(
    Guid DonanteId,
    string Email,
    string Telefono,
    string DireccionFiscal,
    ContactoDonanteDto ContactoPrincipal,
    Guid UsuarioModificacionId);

