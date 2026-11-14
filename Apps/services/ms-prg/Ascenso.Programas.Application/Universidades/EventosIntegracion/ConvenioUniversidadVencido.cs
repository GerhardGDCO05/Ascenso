namespace Ascenso.Programas.Application.Universidades.EventosIntegracion;

public sealed record class ConvenioUniversidadVencido(
    Guid EventoId,
    DateTimeOffset OcurridoEn,
    Guid UniversidadAliadaId,
    Guid ConvenioId,
    string NumeroConvenio,
    bool MantieneConvenioVigente);
