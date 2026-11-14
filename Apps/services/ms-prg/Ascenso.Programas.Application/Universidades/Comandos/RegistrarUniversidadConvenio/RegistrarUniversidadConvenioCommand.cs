using Ascenso.Programas.Application.Universidades.DTOs;

namespace Ascenso.Programas.Application.Universidades.Comandos.RegistrarUniversidadConvenio;

public sealed record class RegistrarUniversidadConvenioCommand(
    string CodigoInstitucional,
    string Nombre,
    string Direccion,
    ContactoEnlaceDto ContactoEnlace,
    CuentaBancariaInstitucionalDto? CuentaBancariaInstitucional,
    Guid? UsuarioEnlaceId,
    string NumeroConvenio,
    string TerminosResumen,
    DateOnly FechaInicioConvenio,
    DateOnly FechaFinConvenio,
    string UrlDocumentoPdfFirebase,
    bool PermiteIntercambioAutomaticoDatos,
    Guid UsuarioCreacionId);
