namespace Ascenso.Programas.Application.Programas.EventosIntegracion;

public sealed record ProgramaBecaCreado(
    Guid EventoId,
    Guid ProgramaBecaId,
    string Codigo,
    string Nombre,
    string ModalidadCobertura,
    int CupoTotal,
    string PeriodoInicialId,
    Guid CorrelacionId,
    DateTimeOffset OcurridoEn);
