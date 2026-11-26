namespace Ascenso.Programas.Application.Convocatorias.Comandos.PublicarConvocatoria;

public sealed record PublicarConvocatoriaCommand(
    Guid ConvocatoriaId,
    Guid UsuarioModificacionId);
