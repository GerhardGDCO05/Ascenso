namespace Ascenso.Programas.Application.Programas.EventosIntegracion;

public sealed record ProgramaBecaConfigurado(
    Guid EventoId,
    Guid ProgramaBecaId,
    string Codigo,
    decimal PresupuestoTotalAsignado,
    int CupoTotal,
    IReadOnlyList<Guid> UniversidadesElegiblesIds,
    IReadOnlyList<Guid> CarrerasElegiblesIds,
    Guid CorrelacionId,
    DateTimeOffset OcurridoEn);
