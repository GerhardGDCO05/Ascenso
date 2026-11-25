namespace Ascenso.Programas.Application.Convocatorias.EventosIntegracion;

public sealed record ConvocatoriaCerrada(
    Guid EventoId,
    Guid ConvocatoriaId,
    Guid ProgramaBecaId,
    string Codigo,
    DateTimeOffset FechaCierre,
    Guid CorrelacionId,
    DateTimeOffset OcurridoEn);
