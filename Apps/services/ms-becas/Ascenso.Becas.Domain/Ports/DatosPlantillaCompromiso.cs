using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Datos necesarios para generar la plantilla del compromiso que firma el
/// becario. Incluye las condiciones del programa y las condiciones económicas
/// de la beca.
/// </summary>
public sealed record DatosPlantillaCompromiso(
    Guid BecaId,
    string CodigoBeca,
    string NombreEstudiante,
    string NumeroDocumentoEstudiante,
    string NombrePrograma,
    string PeriodoCodigo,
    PoliticaCobertura Cobertura,
    decimal MontoPorPeriodo,
    int DuracionPeriodos,
    CondicionesPermanencia Condiciones,
    DateOnly FechaEmision);
