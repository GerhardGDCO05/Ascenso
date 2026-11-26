namespace Ascenso.Programas.Application.Convocatorias.Comandos.AgregarDocumentoRequerido;

public sealed record AgregarDocumentoRequeridoCommand(
    Guid ConvocatoriaId,
    string TipoDocumento,
    string Nombre,
    string? Descripcion,
    bool Obligatorio,
    IReadOnlyList<string> TiposMimePermitidos,
    int TamanioMaximoMb,
    Guid UsuarioModificacionId);
