namespace Ascenso.Programas.Application.Convocatorias.EventosIntegracion;

public sealed record ConvocatoriaPublicada(
    Guid EventoId,
    Guid ConvocatoriaId,
    Guid ProgramaBecaId,
    string Codigo,
    DateTimeOffset FechaApertura,
    DateTimeOffset FechaCierre,
    int CuposAsignados,
    Guid CorrelacionId,
    DateTimeOffset OcurridoEn);
