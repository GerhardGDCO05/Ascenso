using Ascenso.Programas.Application.Convocatorias.DTOs;

namespace Ascenso.Programas.Application.Convocatorias.Comandos.EditarConvocatoriaBorrador;

public sealed record EditarConvocatoriaBorradorCommand(
    Guid ConvocatoriaId,
    string PeriodoAcademicoId,
    string Nombre,
    DateOnly FechaApertura,
    DateOnly FechaCierre,
    DateOnly FechaLimiteSubsanacionDocumentos,
    DateOnly FechaInicioEvaluacion,
    DateOnly FechaCierreEvaluacion,
    bool RequiereRevisionDocumentalPrevia,
    int CuposAsignados,
    CriteriosElegibilidadCicloDto CriteriosElegibilidad,
    Guid UsuarioModificacionId);
