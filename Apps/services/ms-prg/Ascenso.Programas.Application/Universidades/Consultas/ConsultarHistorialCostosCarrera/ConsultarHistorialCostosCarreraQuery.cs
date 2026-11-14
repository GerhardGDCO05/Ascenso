namespace Ascenso.Programas.Application.Universidades.Consultas.ConsultarHistorialCostosCarrera;

public sealed record class ConsultarHistorialCostosCarreraQuery(
    Guid UniversidadAliadaId,
    Guid CarreraElegibleId,
    string? PeriodoAcademicoId = null);
