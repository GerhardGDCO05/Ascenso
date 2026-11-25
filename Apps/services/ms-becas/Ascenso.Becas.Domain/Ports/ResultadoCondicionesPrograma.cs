using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Ports;

public sealed record ResultadoCondicionesPrograma(
    Guid ProgramaBecaId,
    Guid CarreraId,
    Guid PeriodoAcademicoId,
    Guid UniversidadAliadaId,
    PoliticaCobertura PoliticaCobertura,
    CondicionesPermanencia CondicionesPermanencia,
    CostoMatriculaVigente CostoMatricula,
    ConvenioVigente Convenio);

public sealed record CostoMatriculaVigente(
    Guid CostoMatriculaReferenciaId,
    Guid CarreraId,
    Guid PeriodoAcademicoId,
    decimal MontoReferenciaMatricula,
    string Moneda,
    int NumeroVersion,
    DateOnly FechaVigenciaDesde,
    DateOnly? FechaVigenciaHasta,
    string Estado);

public sealed record ConvenioVigente(
    Guid UniversidadAliadaId,
    Guid ConvenioId,
    string Estado,
    DateOnly FechaFin);
