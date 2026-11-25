namespace Ascenso.Becas.Domain.Ports;

public sealed record ResultadoPeriodoAcademico(
    Guid PeriodoAcademicoId,
    string Codigo,
    string Nombre,
    int OrdenSecuencia,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string Estado,
    bool EsActual);
