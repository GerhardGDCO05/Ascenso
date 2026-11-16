namespace Ascenso.Becas.Application.IntegracionEventos;

/// <summary>
/// Payload de <c>ProgramaBecaConfigurado</c> publicado por MS-PRG.
/// </summary>
public sealed record ProgramaBecaConfigurado(
    Guid ProgramaBecaId,
    string Codigo,
    string Nombre,
    PoliticaCoberturaIntegracion PoliticaCobertura,
    CondicionesPermanenciaIntegracion CondicionesPermanencia,
    bool PermiteAcumulacion,
    bool RequiereCompromisoFirmado,
    int DuracionPeriodos,
    string Estado);

/// <summary>
/// Política de cobertura tal como viaja en el evento de configuración.
/// </summary>
public sealed record PoliticaCoberturaIntegracion(
    string Modalidad,
    decimal PorcentajeCobertura,
    decimal MontoFijoPeriodo,
    string Moneda);

/// <summary>
/// Condiciones de permanencia tal como viajan en el evento de configuración.
/// </summary>
public sealed record CondicionesPermanenciaIntegracion(
    decimal PromedioMinimoRenovacion,
    int MateriasMinimasAprobadas,
    int MaximoMateriasReprobadas,
    int HorasServicioPorPeriodo,
    string ConsecuenciaIncumplimiento);

/// <summary>
/// Payload de <c>CostoMatriculaActualizado</c> publicado por MS-PRG.
/// </summary>
public sealed record CostoMatriculaActualizado(
    Guid CostoMatriculaReferenciaId,
    Guid CarreraId,
    Guid PeriodoAcademicoId,
    decimal MontoReferenciaMatricula,
    string Moneda,
    int NumeroVersion,
    DateOnly FechaVigenciaDesde,
    DateOnly? FechaVigenciaHasta,
    string Estado);

/// <summary>
/// Payload de <c>ConvenioUniversidadRegistrado</c> publicado por MS-PRG.
/// </summary>
public sealed record ConvenioUniversidadRegistrado(
    Guid UniversidadAliadaId,
    Guid ConvenioId,
    string Estado,
    DateOnly FechaFin);

/// <summary>
/// Payload de <c>ConvenioUniversidadVencido</c> publicado por MS-PRG.
/// </summary>
public sealed record ConvenioUniversidadVencido(
    Guid UniversidadAliadaId,
    Guid ConvenioId,
    string Estado,
    DateOnly FechaFin);
