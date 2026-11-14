namespace Ascenso.Programas.Application.Universidades.Comandos.RegistrarNuevoConvenio;

public sealed record class RegistrarNuevoConvenioCommand(
    Guid UniversidadAliadaId,
    string NumeroConvenio,
    string TerminosResumen,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string UrlDocumentoPdfFirebase,
    bool PermiteIntercambioAutomaticoDatos,
    Guid UsuarioModificacionId);
