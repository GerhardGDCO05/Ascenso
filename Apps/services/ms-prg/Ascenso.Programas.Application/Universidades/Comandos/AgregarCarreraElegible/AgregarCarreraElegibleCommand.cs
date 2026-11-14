namespace Ascenso.Programas.Application.Universidades.Comandos.AgregarCarreraElegible;

public sealed record class AgregarCarreraElegibleCommand(
    Guid UniversidadAliadaId,
    string CodigoCarrera,
    string Nombre,
    int DuracionSemestres,
    Guid UsuarioModificacionId);
