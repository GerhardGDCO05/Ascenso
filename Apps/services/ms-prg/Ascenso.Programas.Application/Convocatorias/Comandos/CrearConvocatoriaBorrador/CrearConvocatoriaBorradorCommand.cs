using Ascenso.Programas.Application.Convocatorias.DTOs;

namespace Ascenso.Programas.Application.Convocatorias.Comandos.CrearConvocatoriaBorrador;

public sealed record CrearConvocatoriaBorradorCommand(
    Guid ProgramaBecaId,
    string PeriodoAcademicoId,
    string Codigo,
    string Nombre,
    DateOnly FechaApertura,
    DateOnly FechaCierre,
    DateOnly FechaLimiteSubsanacionDocumentos,
    DateOnly FechaInicioEvaluacion,
    DateOnly FechaCierreEvaluacion,
    bool RequiereRevisionDocumentalPrevia,
    int CuposAsignados,
    CriteriosElegibilidadCicloDto CriteriosElegibilidad,
    Guid UsuarioCreacionId);
