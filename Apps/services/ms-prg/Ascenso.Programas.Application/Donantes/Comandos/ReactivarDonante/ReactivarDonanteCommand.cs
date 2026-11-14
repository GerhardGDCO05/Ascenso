namespace Ascenso.Programas.Application.Donantes.Comandos.ReactivarDonante;

public sealed record class ReactivarDonanteCommand(
    Guid DonanteId,
    Guid UsuarioModificacionId);

