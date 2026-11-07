namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Respuesta del servicio de postulaciones sobre la adjudicabilidad.
/// </summary>
public sealed record ResultadoAdjudicabilidad(
    Guid PostulacionId,
    bool Adjudicable,
    Guid ProgramaBecaId,
    Guid PeriodoAcademicoId,
    string PeriodoCodigo,
    Guid UniversidadAliadaId,
    Guid CarreraId);
