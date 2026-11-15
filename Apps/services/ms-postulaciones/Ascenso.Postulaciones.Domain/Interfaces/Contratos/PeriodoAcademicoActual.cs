using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Interfaces.Contratos;

/// <summary>
/// Datos mínimos del periodo académico vigente obtenidos del calendario
/// institucional.
/// </summary>
public sealed record class PeriodoAcademicoActual(
    Guid Id,
    string Nombre,
    EstadoPeriodoAcademico Estado);
