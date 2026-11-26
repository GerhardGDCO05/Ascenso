namespace Ascenso.Programas.Application.Convocatorias.DTOs;

public sealed record CriteriosElegibilidadCicloDto(
    decimal PromedioMinimo,
    int EdadMinima,
    int EdadMaxima,
    decimal TopeIngresoFamiliar,
    IReadOnlyList<Guid> UniversidadesPermitidasIds,
    IReadOnlyList<Guid> CarrerasPermitidasIds);

public sealed record DocumentoRequeridoDto(
    Guid Id,
    string TipoDocumento,
    string Nombre,
    string? Descripcion,
    bool Obligatorio,
    IReadOnlyList<string> TiposMimePermitidos,
    int TamanioMaximoMb);

public sealed record ConvocatoriaResumenDto(
    Guid Id,
    Guid ProgramaBecaId,
    string Codigo,
    string Nombre,
    string PeriodoAcademicoId,
    string Estado,
    DateTimeOffset FechaApertura,
    DateTimeOffset FechaCierre,
    int CuposAsignados,
    DateTimeOffset? FechaPublicacion);

public sealed record ConvocatoriaDetalleDto(
    Guid Id,
    Guid ProgramaBecaId,
    string Codigo,
    string Nombre,
    string PeriodoAcademicoId,
    string Estado,
    DateTimeOffset FechaApertura,
    DateTimeOffset FechaCierre,
    DateTimeOffset FechaLimiteSubsanacionDocumentos,
    DateTimeOffset FechaInicioEvaluacion,
    DateTimeOffset FechaCierreEvaluacion,
    bool RequiereRevisionDocumentalPrevia,
    int CuposAsignados,
    CriteriosElegibilidadCicloDto CriteriosElegibilidad,
    IReadOnlyList<DocumentoRequeridoDto> DocumentosRequeridos,
    DateTimeOffset? FechaPublicacion,
    DateTimeOffset FechaCreacion,
    DateTimeOffset FechaUltimaModificacion);

public sealed record ConvocatoriaAbiertaDto(
    Guid Id,
    Guid ProgramaBecaId,
    string Codigo,
    string Nombre,
    string PeriodoAcademicoId,
    DateTimeOffset FechaApertura,
    DateTimeOffset FechaCierre,
    DateTimeOffset FechaLimiteSubsanacionDocumentos,
    DateTimeOffset FechaInicioEvaluacion,
    DateTimeOffset FechaCierreEvaluacion,
    bool RequiereRevisionDocumentalPrevia,
    int CuposAsignados,
    CriteriosElegibilidadCicloDto CriteriosElegibilidad,
    IReadOnlyList<DocumentoRequeridoDto> DocumentosRequeridos);
