namespace Ascenso.Programas.Application.Universidades.EventosIntegracion;

public sealed record class ConvenioUniversidadRegistrado(
    Guid EventoId,
    DateTimeOffset OcurridoEn,
    Guid UniversidadAliadaId,
    Guid ConvenioId,
    string CodigoInstitucional,
    string NumeroConvenio,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string UrlDocumentoPdfFirebase);
