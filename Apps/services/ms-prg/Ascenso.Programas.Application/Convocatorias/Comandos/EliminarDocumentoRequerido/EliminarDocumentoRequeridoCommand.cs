namespace Ascenso.Programas.Application.Convocatorias.Comandos.EliminarDocumentoRequerido;

public sealed record EliminarDocumentoRequeridoCommand(
    Guid ConvocatoriaId,
    Guid DocumentoId,
    Guid UsuarioModificacionId);
