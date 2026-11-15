using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Proyección local de la configuración vigente de un programa de becas.
/// </summary>
public sealed record ProgramaOtorgamientoProyeccion(
    Guid ProgramaBecaId,
    string Codigo,
    string Nombre,
    PoliticaCobertura PoliticaCobertura,
    CondicionesPermanencia CondicionesPermanencia,
    bool PermiteAcumulacion,
    bool RequiereCompromisoFirmado,
    int DuracionPeriodos,
    string Estado);
