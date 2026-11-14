namespace Ascenso.Programas.Application.Universidades.Comandos.MarcarConvenioVencido;

public sealed record class MarcarConvenioVencidoCommand(
    Guid UniversidadAliadaId,
    Guid ConvenioId,
    Guid UsuarioModificacionId);
