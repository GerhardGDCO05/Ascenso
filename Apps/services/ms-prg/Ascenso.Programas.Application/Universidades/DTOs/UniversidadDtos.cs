namespace Ascenso.Programas.Application.Universidades.DTOs;

public sealed record class ContactoEnlaceDto(
    string Nombre,
    string Email,
    string Telefono);

public sealed record class CuentaBancariaInstitucionalDto(
    string Banco,
    string TipoCuenta,
    string NumeroCuentaCifrado,
    string RifTitular);

public sealed record class ConvenioDto(
    Guid Id,
    Guid UniversidadAliadaId,
    string NumeroConvenio,
    string TerminosResumen,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string UrlDocumentoPdfFirebase,
    bool PermiteIntercambioAutomaticoDatos,
    string Estado,
    DateTimeOffset FechaCreacion);

public sealed record class CostoMatriculaReferenciaDto(
    Guid Id,
    Guid CarreraElegibleId,
    string PeriodoAcademicoId,
    int NumeroVersion,
    decimal MontoReferenciaMatricula,
    string Moneda,
    DateTimeOffset FechaVigenciaDesde,
    DateTimeOffset? FechaVigenciaHasta,
    string Estado);

public sealed record class CarreraElegibleDto(
    Guid Id,
    Guid UniversidadAliadaId,
    string CodigoCarrera,
    string Nombre,
    int DuracionSemestres,
    bool Activa);

public sealed record class UniversidadResumenDto(
    Guid Id,
    string CodigoInstitucional,
    string Nombre,
    string Estado,
    bool TieneConvenioVigente,
    int TotalCarreras,
    int Version);

public sealed record class UniversidadDetalleDto(
    Guid Id,
    string CodigoInstitucional,
    string Nombre,
    string Direccion,
    ContactoEnlaceDto ContactoEnlace,
    CuentaBancariaInstitucionalDto? CuentaBancariaInstitucional,
    Guid? UsuarioEnlaceId,
    string Estado,
    bool TieneConvenioVigente,
    int Version,
    DateTimeOffset FechaCreacion,
    DateTimeOffset FechaUltimaModificacion,
    IReadOnlyList<ConvenioDto> Convenios,
    IReadOnlyList<CarreraElegibleDto> Carreras);
