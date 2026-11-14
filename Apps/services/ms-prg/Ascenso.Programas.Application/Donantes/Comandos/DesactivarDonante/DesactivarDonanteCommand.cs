namespace Ascenso.Programas.Application.Donantes.Comandos.DesactivarDonante;

public sealed record class DesactivarDonanteCommand(
    Guid DonanteId,
    Guid UsuarioModificacionId);

