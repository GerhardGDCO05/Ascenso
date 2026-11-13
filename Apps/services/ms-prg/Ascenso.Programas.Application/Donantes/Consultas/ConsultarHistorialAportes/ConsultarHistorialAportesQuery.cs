namespace Ascenso.Programas.Application.Donantes.Consultas.ConsultarHistorialAportes;

public sealed record class ConsultarHistorialAportesQuery(
    Guid DonanteId,
    string? PeriodoAcademicoId = null);

