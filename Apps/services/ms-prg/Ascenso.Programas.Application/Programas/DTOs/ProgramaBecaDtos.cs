namespace Ascenso.Programas.Application.Programas.DTOs;

public sealed record PoliticaCoberturaDto(
    string Modalidad,
    decimal PorcentajeCobertura,
    decimal MontoFijoPeriodo,
    string Moneda);

public sealed record CriteriosElegibilidadProgramaDto(
    decimal PromedioMinimoIngreso,
    int EdadMinima,
    int EdadMaxima,
    decimal TopeIngresoFamiliarMensual,
    IReadOnlyList<Guid> UniversidadesElegiblesIds,
    IReadOnlyList<Guid> CarrerasElegiblesIds);

public sealed record CondicionesPermanenciaDto(
    decimal PromedioMinimoRenovacion,
    int MateriasMinimasAprobadas,
    int MaximoMateriasReprobadas,
    int HorasServicioPorPeriodo,
    string FrecuenciaReportes,
    string ConsecuenciaIncumplimiento,
    decimal MargenRiesgoPromedio,
    int DuracionMaximaPausaPeriodos,
    bool PermiteRenovacionTardiaExcepcional);

public sealed record AsignacionFondoProgramaDto(
    Guid Id,
    Guid DonanteId,
    string PeriodoAcademicoId,
    decimal MontoAsignado,
    string Estado,
    DateTimeOffset FechaAsignacion,
    Guid AutorizadoPorUsuarioId);

public sealed record ProgramaBecaResumenDto(
    Guid Id,
    string Codigo,
    string Nombre,
    string ModalidadCobertura,
    int CupoTotal,
    int CuposOcupados,
    string PeriodoInicialId,
    decimal PresupuestoTotalAsignado,
    string Moneda,
    string Estado,
    DateOnly VigenciaDesde,
    DateOnly VigenciaHasta);

public sealed record ProgramaBecaDetalleDto(
    Guid Id,
    string Codigo,
    string Nombre,
    string Descripcion,
    PoliticaCoberturaDto PoliticaCobertura,
    int CupoTotal,
    int CuposOcupados,
    int DuracionPeriodos,
    string PeriodoInicialId,
    DateOnly VigenciaDesde,
    DateOnly VigenciaHasta,
    bool PermiteAcumulacion,
    bool RequiereCompromisoFirmado,
    string? TerminosCompromisoEspecial,
    decimal PresupuestoTotalAsignado,
    string Moneda,
    string Estado,
    CriteriosElegibilidadProgramaDto? CriteriosElegibilidad,
    CondicionesPermanenciaDto? CondicionesPermanencia,
    IReadOnlyList<AsignacionFondoProgramaDto> AsignacionesFondo,
    DateTimeOffset FechaCreacion,
    DateTimeOffset FechaUltimaModificacion);
