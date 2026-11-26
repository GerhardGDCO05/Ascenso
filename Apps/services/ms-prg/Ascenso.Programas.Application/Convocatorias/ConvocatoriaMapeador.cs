using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Domain.Convocatorias.Agregados;
using Ascenso.Programas.Domain.Convocatorias.Entidades;
using Ascenso.Programas.Domain.Convocatorias.ObjetosValor;
using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Application.Convocatorias;

/// <summary>
/// Mapeador del agregado de convocatorias hacia los contratos de respuesta.
/// </summary>
internal static class ConvocatoriaMapeador
{
    public static ConvocatoriaResumenDto AResumenDto(Convocatoria convocatoria)
        => new(
            convocatoria.Id,
            convocatoria.ProgramaBecaId,
            convocatoria.Codigo,
            convocatoria.Nombre,
            convocatoria.PeriodoAcademicoId,
            convocatoria.Estado.ACodigo(),
            convocatoria.FechaApertura,
            convocatoria.FechaCierre,
            convocatoria.CuposAsignados,
            convocatoria.FechaPublicacion);

    public static ConvocatoriaDetalleDto ADetalleDto(Convocatoria convocatoria)
        => new(
            convocatoria.Id,
            convocatoria.ProgramaBecaId,
            convocatoria.Codigo,
            convocatoria.Nombre,
            convocatoria.PeriodoAcademicoId,
            convocatoria.Estado.ACodigo(),
            convocatoria.FechaApertura,
            convocatoria.FechaCierre,
            convocatoria.FechaLimiteSubsanacionDocumentos,
            convocatoria.FechaInicioEvaluacion,
            convocatoria.FechaCierreEvaluacion,
            convocatoria.RequiereRevisionDocumentalPrevia,
            convocatoria.CuposAsignados,
            ADto(convocatoria.CriteriosElegibilidad),
            convocatoria.DocumentosRequeridos.Select(ADto).ToArray(),
            convocatoria.FechaPublicacion,
            convocatoria.FechaCreacion,
            convocatoria.FechaUltimaModificacion);

    public static ConvocatoriaAbiertaDto AAbiertaDto(Convocatoria convocatoria)
        => new(
            convocatoria.Id,
            convocatoria.ProgramaBecaId,
            convocatoria.Codigo,
            convocatoria.Nombre,
            convocatoria.PeriodoAcademicoId,
            convocatoria.FechaApertura,
            convocatoria.FechaCierre,
            convocatoria.FechaLimiteSubsanacionDocumentos,
            convocatoria.FechaInicioEvaluacion,
            convocatoria.FechaCierreEvaluacion,
            convocatoria.RequiereRevisionDocumentalPrevia,
            convocatoria.CuposAsignados,
            ADto(convocatoria.CriteriosElegibilidad),
            convocatoria.DocumentosRequeridos.Select(ADto).ToArray());

    public static DocumentoRequeridoDto ADto(DocumentoRequerido documento)
        => new(
            documento.Id,
            documento.TipoDocumento,
            documento.Nombre,
            documento.Descripcion,
            documento.Obligatorio,
            documento.TiposMimePermitidos,
            documento.TamanioMaximoMb);

    public static CriteriosElegibilidadCicloDto ADto(CriteriosElegibilidadCiclo criterios)
        => new(
            criterios.PromedioMinimo,
            criterios.EdadMinima,
            criterios.EdadMaxima,
            criterios.TopeIngresoFamiliar,
            criterios.UniversidadesPermitidasIds,
            criterios.CarrerasPermitidasIds);
}
